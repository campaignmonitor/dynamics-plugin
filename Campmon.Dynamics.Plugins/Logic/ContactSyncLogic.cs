using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Newtonsoft.Json;
using Campmon.Dynamics.Logic;
using System.Collections.Generic;
using createsend_dotnet;

namespace Campmon.Dynamics.Plugins.Logic
{
    public class ContactSyncLogic
    {
        private ITracingService tracer;
        private IOrganizationService orgService;
        private CampaignMonitorConfiguration campaignMonitorConfig;

        public ContactSyncLogic(IOrganizationService organizationService, ITracingService trace)
        {
            orgService = organizationService;
            tracer = trace;

            // Retrieve the Campaign Monitor Configuration record.
            ConfigurationService configService = new ConfigurationService(orgService, tracer);
            campaignMonitorConfig = configService.VerifyAndLoadConfig();
        }

        internal void SyncContact(Entity target, Entity postImage, bool isUpdate)
        {
            if (target == null || postImage == null)
            {
                tracer.Trace("Invalid target or postImage entity.");
                return;
            }

            if (campaignMonitorConfig == null)
            {
                tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            string emailField = SharedLogic.GetPrimaryEmailField(campaignMonitorConfig.SubscriberEmail);
            if (string.IsNullOrWhiteSpace(emailField) ||
                    !postImage.Contains(emailField) || string.IsNullOrWhiteSpace(postImage[emailField].ToString()))
            {
                tracer.Trace("The email field to sync is missing or contains invalid data.");
                return;
            }
           
            if (campaignMonitorConfig.SyncViewId != Guid.Empty)
            {
                tracer.Trace("Testing the contact against the filter.");

                // Retrieve the view specified in the campmon_syncviewid field of the configuration record.
                var filterQuery = SharedLogic.GetConfigFilterQuery(orgService, campaignMonitorConfig.SyncViewId);

                // Modify the sync view fetch query to include a filter condition for the current contact id.Execute the modified query and check if the contact is returned.If it is, exit the plugin.
                if (!TestContactFitsFilter(filterQuery, target.Id))
                {
                    tracer.Trace("Contact does not fit the filter.");
                    return;
                }
            }
            else
            {
                if (target.Contains("statecode") && (target["statecode"] as OptionSetValue).Value == 1)
                {
                    tracer.Trace("Contact was not synced: no view is selected and this contact is deactivated.");
                    return;
                }
            }

            tracer.Trace("Contact fits the filter, or no filter is selected.");

            /*
                Create a campmon_message record with the following data:
                    • campmon_sdkmessage = Plugin message (create or update)
                    • campmon_data = JSON serialized sync data
            */
            var syncMessage = new Entity("campmon_message");
            var fields = SharedLogic.ContactAttributesToSubscriberFields(orgService, tracer, target, campaignMonitorConfig.SyncFields.ToList());            

            // Check that the plugin target has modified attributes that are included in the campmon_syncfields data. If there are not any sync fields in the target, exit the plugin.
            if (fields.Count <= 0)
            {
                tracer.Trace("There are no fields in the target that match the current fields being synced with Campaign Monitor.");
                return;
            }

            if (isUpdate && !target.Attributes.Contains(emailField))
            {
                fields.Add(new SubscriberCustomField { Key = emailField, Value = postImage[emailField].ToString() });
            }
            else
            {
                // TODO: if it contains primary email and isUpdate, then the primary email was changed and we need to do something to handle that
            }

            if (isUpdate && !target.Attributes.Contains("fullname"))
            {
                fields.Add(new SubscriberCustomField { Key = "fullname", Value = postImage["fullname"].ToString() });
            }

            var syncData = JsonConvert.SerializeObject(fields);

            syncMessage["campmon_name"] = isUpdate ? "update" : "create";
            syncMessage["campmon_data"] = syncData;
            syncMessage["campmon_email"] = emailField;
            orgService.Create(syncMessage);
        }

        internal bool TestContactFitsFilter(QueryExpression filter, Guid contactID)
        {            
            ConditionExpression contactCondition = new ConditionExpression("contactid", ConditionOperator.Equal, contactID);
            filter.Criteria.AddCondition(contactCondition);

            var contacts = orgService.RetrieveMultiple(filter).Entities;
            return contacts.Count >= 1;
        }
    }
}