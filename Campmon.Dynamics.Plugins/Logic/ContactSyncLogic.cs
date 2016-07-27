using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Newtonsoft.Json;
using Campmon.Dynamics.Logic;

namespace Campmon.Dynamics.Plugins.Logic
{
    public class ContactSyncLogic
    {
        private ITracingService _tracer;
        private IOrganizationService _orgService;

        public ContactSyncLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        internal void SyncContact(Entity target, Entity postImage, bool isUpdate)
        {
            if (target == null || postImage == null)
            {
                return;
            }

            // Retrieve the Campaign Monitor Configuration record. If it does not exist or is missing access token or client id, exit the plugin.
            ConfigurationService configService = new ConfigurationService(_orgService);
            CampaignMonitorConfiguration campaignMonitorConfig = configService.VerifyAndLoadConfig();
            if (campaignMonitorConfig == null ||
                    campaignMonitorConfig.AccessToken == null || campaignMonitorConfig.ClientId == null)
            {
                _tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            string emailField = SharedLogic.GetPrimaryEmailField(campaignMonitorConfig.SubscriberEmail);                                              
            if (string.IsNullOrWhiteSpace(emailField) || 
                    !postImage.Contains(emailField) || string.IsNullOrWhiteSpace(postImage[emailField].ToString()))
            {
                _tracer.Trace("The email field to sync is missing or contains invalid data.");
                return;
            }

            // Retrieve the view specified in the campmon_syncviewid field of the configuration record.
            var filterQuery = SharedLogic.GetConfigFilterQuery(_orgService, campaignMonitorConfig.SyncViewId);            

            // Modify the sync view fetch query to include a filter condition for the current contact id.Execute the modified query and check if the contact is returned.If it is, exit the plugin.
            if (!TestContactFitsFilter(filterQuery, target.Id))
            {
                _tracer.Trace("Contact does not fit the filter.");
                return;
            }

            /*
                Create a campmon_message record with the following data:
                    • campmon_sdkmessage = Plugin message (create or update)
                    • campmon_data = JSON serialized sync data
            */
            var syncMessage = new Entity("campmon_message");
            var fields = SharedLogic.ContactAttributesToSubscriberFields(_orgService, target, campaignMonitorConfig.SyncFields.ToList());

            var syncData = fields.Count > 0
                ? JsonConvert.SerializeObject(fields)
                : string.Empty;

            // If this is an update operation, check that the plugin target has modified attributes that are included in the campmon_syncfields data. If there are not any sync fields in the target, exit the plugin.
            if (string.IsNullOrWhiteSpace(syncData))
            {
                _tracer.Trace("There are no fields in the target that match the current fields being synced with Campaign Monitor.");
                return;
            }
            
            syncMessage["campmon_name"] = isUpdate ? "update" : "create";
            syncMessage["campmon_data"] = syncData;
            syncMessage["campmon_email"] = emailField;
            _orgService.Create(syncMessage);
        }

        internal bool TestContactFitsFilter(QueryExpression filter, Guid contactID)
        {            
            ConditionExpression contactCondition = new ConditionExpression("contactid", ConditionOperator.Equal, contactID);
            filter.Criteria.AddCondition(contactCondition);

            var contacts = _orgService.RetrieveMultiple(filter).Entities;
            return contacts.Count >= 1;
        }
    }
}