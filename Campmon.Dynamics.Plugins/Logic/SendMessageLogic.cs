using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk;
using createsend_dotnet;
using Newtonsoft.Json;

namespace Campmon.Dynamics.Plugins.Logic
{
    class SendMessageLogic
    {
        IOrganizationService _orgService;
        ITracingService _tracer;

        CampaignMonitorConfiguration _campaignMonitorConfig;
        AuthenticationDetails _authDetails;

        public SendMessageLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;

            ConfigurationService configService = new ConfigurationService(orgService);
            _campaignMonitorConfig = configService.LoadConfig();
            _authDetails = new ApiKeyAuthenticationDetails(_campaignMonitorConfig.AccessToken);
        }

        public void SendMessage(Entity target)
        {            
            if (!_campaignMonitorConfig.SyncDuplicateEmails)
            {
                // If campmon_syncduplicates = 'false', do a retrieve multiple for any contact that matches the email address found in the sync email of the contact.
                // if yes : set campmon_error on message to "Duplicate email"
            }

            List<SubscriberCustomField> fields = PrettifySchemaNames(target);

            try
            {
                SendSubscriberToList(_campaignMonitorConfig.ListId, fields);
            }
            catch (Exception ex)
            {
                target["campmon_error"] = ex.Message;
                _orgService.Update(target);
                return;
            }

            // deactivate msg if successful create/update
            target["statecode"] = 1;
            target["statuscode"] = 2; //
            _orgService.Update(target);
        }

        internal List<SubscriberCustomField> PrettifySchemaNames(Entity target)
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field

            RetrieveEntityRequest getEntityMetadataRequest = new RetrieveEntityRequest
            {
                LogicalName = "contact",
                RetrieveAsIfPublished = true
            };
            RetrieveEntityResponse entityMetaData = (RetrieveEntityResponse)_orgService.Execute(getEntityMetadataRequest);

            List<SubscriberCustomField> fields = JsonConvert.DeserializeObject<List<SubscriberCustomField>>(target["campmon_data"].ToString());

            foreach (var field in fields)
            {
                var displayName = from x in entityMetaData.EntityMetadata.Attributes
                                  where x.LogicalName == field.Key
                                  select x.DisplayName.ToString();

                if (displayName.Any())
                {
                    field.Key = displayName.First().ToString();
                }
            }

            return fields;
        }

        internal void SendSubscriberToList(string listId, List<SubscriberCustomField> fields)
        {
            // placeholder code to get name/email
            var name = fields.Where(f => f.Key == "name").FirstOrDefault();
            var email = fields.Where(f => f.Key == "email").FirstOrDefault();
            if (name != null) fields.Remove(name);
            if (email != null) fields.Remove(email);
                 
            // send subscriber to campaign monitor list using CM API
            Subscriber subscriber = new Subscriber(_authDetails, listId);
            subscriber.Add(
                    email != null ? email.Value : string.Empty, 
                    name != null ? name.Value : string.Empty, 
                    fields, 
                    false); // resubscribe == false?
        }
    }
}
