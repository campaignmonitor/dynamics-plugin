using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using createsend_dotnet;
using Newtonsoft.Json;
using Campmon.Dynamics.Logic;

namespace Campmon.Dynamics.Plugins.Logic
{
    public class SendMessageLogic
    {
        private IOrganizationService orgService;
        private ITracingService tracer;
        private CampaignMonitorConfiguration campaignMonitorConfig;
        private AuthenticationDetails authDetails;

        public SendMessageLogic(IOrganizationService organizationService, ITracingService trace)
        {
            orgService = organizationService;
            tracer = trace;

            ConfigurationService configService = new ConfigurationService(orgService, tracer);
            campaignMonitorConfig = configService.VerifyAndLoadConfig();
            authDetails = Authenticator.GetAuthentication(campaignMonitorConfig);
        }

        public void SendMessage(Entity target)
        {
            var emailField = target["campmon_email"].ToString();
            List<SubscriberCustomField> contactData = JsonConvert.DeserializeObject<List<SubscriberCustomField>>(target["campmon_data"].ToString());

            // If campmon_syncduplicates = 'false', do a retrieve multiple for any contact
            //      that matches the email address found in the sync email of the contact.
            // if yes : set campmon_error on message to "Duplicate email"
            if (!campaignMonitorConfig.SyncDuplicateEmails)
            {
                var contactEmail = contactData.Where(x => x.Key == emailField).FirstOrDefault();
                if (contactEmail == null)
                {
                    tracer.Trace("The email field to sync was not found within the data for this message.");
                    target["campmon_error"] = "The email field to sync was not found within the data for this message.";
                    orgService.Update(target);
                    return;                        
                }

                bool emailIsDuplicate = SharedLogic.CheckEmailIsDuplicate(orgService, emailField, contactEmail.Value.ToString());
                if (emailIsDuplicate)
                {
                    tracer.Trace("Duplicate email");
                    target["campmon_error"] = "Duplicate email";
                    orgService.Update(target);
                    return;
                }                
            }

            try
            {
                SendSubscriberToList(campaignMonitorConfig.ListId, emailField, contactData, target["campmon_name"].ToString().ToLower() == "update");
            }
            catch (Exception ex)
            {
                target["campmon_error"] = ex.Message;
                orgService.Update(target);
                return;
            }

            // deactivate msg if successful create/update
            target["statuscode"] = new OptionSetValue(2);
            target["statecode"] = new OptionSetValue(1);
            orgService.Update(target);
        }

        private void SendSubscriberToList(string listId, string emailField, List<SubscriberCustomField> fields, bool isUpdate)
        {
            // send subscriber to campaign monitor list using CM API
            var name = fields.Where(f => f.Key == "Full Name").FirstOrDefault();
            var email = fields.Where(f => f.Key == emailField).FirstOrDefault();
                        
            tracer.Trace(String.Format("Sending {0}:{1}", email?.Value, name?.Value));

            Subscriber subscriber = new Subscriber(authDetails, listId);

            //if (isUpdate)
            //{
            //    // second field is new email if it changes
            //    subscriber.Update(email?.Value, email?.Value, name?.Value, fields, false, false);
            //}
            //else
            //{
                subscriber.Add(email?.Value, name?.Value, fields, false, false);
            //}
            tracer.Trace("Sent.");
        }
    }
}
