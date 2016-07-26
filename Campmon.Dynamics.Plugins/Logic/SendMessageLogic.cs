using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using createsend_dotnet;
using Newtonsoft.Json;
using Campmon.Dynamics.Logic;

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
            _authDetails = SharedLogic.GetAuthentication(_campaignMonitorConfig);
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
                var contactEmail = contactData.Where(x => x.Key == emailField).FirstOrDefault();
                if (contactEmail == null)
                {
                    target["campmon_error"] = "The email field to sync was not found within the data for this message.";
                    _orgService.Update(target);
                    return;                        
                }

                bool emailIsDuplicate = SharedLogic.CheckEmailIsDuplicate(_orgService, emailField, contactEmail.Value.ToString());
                if (emailIsDuplicate)
                {
                    target["campmon_error"] = "Duplicate email";
                    _orgService.Update(target);
                    return;
                }                
            }

            try
            {
                SendSubscriberToList(_campaignMonitorConfig.ListId, emailField, contactData);
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
