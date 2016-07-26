using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk.Query;

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
            var emailField = target["campmon_email"].ToString();
            List<SubscriberCustomField> contactData = JsonConvert.DeserializeObject<List<SubscriberCustomField>>(target["campmon_data"].ToString());

            // If campmon_syncduplicates = 'false', do a retrieve multiple for any contact
            //      that matches the email address found in the sync email of the contact.
            // if yes : set campmon_error on message to "Duplicate email"
            if (!_campaignMonitorConfig.SyncDuplicateEmails)
            {
                try
                {
                    if (ContainsDuplicateEmail(emailField, contactData))
                    {
                        target["campmon_error"] = "Duplicate email";
                        _orgService.Update(target);
                        return;
                    }
                }
                catch (KeyNotFoundException ex)
                {
                    target["campmon_error"] = ex.Message;
                    _orgService.Update(target);
                    return;
                }
            }

            var fields = PrettifySchemaNames(contactData);
            try
            {
                SendSubscriberToList(_campaignMonitorConfig.ListId, emailField, fields);
            }
            catch (Exception ex)
            {
                target["campmon_error"] = ex.Message;
                _orgService.Update(target);
                return;
            }

            // deactivate msg if successful create/update
            target["statecode"] = 1;
            _orgService.Update(target);
        }

        private bool ContainsDuplicateEmail(string emailField, List<SubscriberCustomField> contactData)
        {
            var contactEmail = contactData.Where(x => x.Key == emailField).FirstOrDefault();

            if (contactEmail == null)
            {
                throw new KeyNotFoundException("The email field to sync was not found within the data for this message.");
            }

            QueryExpression query = new QueryExpression("contact");
            query.Criteria.AddCondition(new ConditionExpression(emailField, ConditionOperator.Equal, contactEmail.Value));
            query.ColumnSet.AddColumn("contactid");
            query.TopCount = 2;

            var set = _orgService.RetrieveMultiple(query);
            return set.TotalRecordCount > 1;
        }

        private List<SubscriberCustomField> PrettifySchemaNames(List<SubscriberCustomField> fields)
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field
            RetrieveEntityRequest getEntityMetadataRequest = new RetrieveEntityRequest
            {
                LogicalName = "contact",
                RetrieveAsIfPublished = true
            };
            RetrieveEntityResponse entityMetaData = (RetrieveEntityResponse)_orgService.Execute(getEntityMetadataRequest);

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

        private void SendSubscriberToList(string listId, string emailField, List<SubscriberCustomField> fields)
        {
            // send subscriber to campaign monitor list using CM API
            var name = fields.Where(f => f.Key == "fullname").FirstOrDefault();
            var email = fields.Where(f => f.Key == emailField).FirstOrDefault();            
            fields.Remove(name);            
            fields.Remove(email);            
            
            Subscriber subscriber = new Subscriber(_authDetails, listId);
            subscriber.Add(
                    email != null ? email.Value : string.Empty,
                    name != null ? name.Value : string.Empty,
                    fields,
                    false); // resubscribe
        }
    }
}
