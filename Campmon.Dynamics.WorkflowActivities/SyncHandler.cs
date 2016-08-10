using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Diagnostics;
using Campmon.Dynamics.Logic;
using Campmon.Dynamics.WorkflowActivities.Model;
using Newtonsoft.Json;
using createsend_dotnet;
using System.Collections.Generic;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class SyncHandler
    {
        private const int BATCH_AMOUNT = 1000;

        private IOrganizationService orgService;
        private ITracingService trace;
        private Stopwatch timer;
        private CampaignMonitorConfiguration config;
        private ConfigurationService configService;

        public SyncHandler(IOrganizationService organizationService, ITracingService tracingService, Stopwatch executionTimer)
        {
            orgService = organizationService;
            trace = tracingService;
            timer = executionTimer;

            configService = new ConfigurationService(orgService, trace);
            trace.Trace("Loading configuration.");
            config = configService.VerifyAndLoadConfig();
        }

        public bool Run()
        {
            trace.Trace("Deserializing bulk sync data.");

            BulkSyncData syncData;
            if (config.BulkSyncData != null)
            {
                syncData = JsonConvert.DeserializeObject<BulkSyncData>(config.BulkSyncData);
            }
            else
            {
                syncData = new BulkSyncData();
            }
            var primaryEmail = SharedLogic.GetPrimaryEmailField(config.SubscriberEmail);

            // retrieve contacts based on the filter, grabbing the columns specified either in the fields to sync (on config entity)
            // or fields specified in the bulkdata sync fields
            QueryExpression viewFilter;

            if (config.SyncViewId != null && config.SyncViewId != Guid.Empty)
            {
                viewFilter = SharedLogic.GetConfigFilterQuery(orgService, config.SyncViewId);
            }
            else
            {
                // if no view filter, sync all active contacts
                viewFilter = new QueryExpression("contact");
                viewFilter.Criteria.AddCondition(
                    new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            }

            viewFilter.ColumnSet.Columns.Clear();

            trace.Trace("Setting columns on filter");
            if (syncData.UpdatedFields != null && syncData.UpdatedFields.Length > 0)
            {
                viewFilter.ColumnSet.Columns.AddRange(syncData.UpdatedFields);
            }
            else
            {
                viewFilter.ColumnSet.Columns.AddRange(config.SyncFields);
            }

            // add required fields for syncing if they are not a part of the filter
            if (!viewFilter.ColumnSet.Columns.Contains(primaryEmail))
            {
                viewFilter.ColumnSet.Columns.Add(primaryEmail);
            }

            if (!viewFilter.ColumnSet.Columns.Contains("fullname"))
            {
                viewFilter.ColumnSet.Columns.Add("fullname");
            }

            viewFilter.AddOrder("modifiedon", OrderType.Ascending);
            viewFilter.TopCount = BATCH_AMOUNT;

            var auth = Authenticator.GetAuthentication(config);
            var sub = new Subscriber(auth, config.ListId);

            trace.Trace("Beginning the sync process.");

            do
            {
                viewFilter.PageInfo.PageNumber = syncData.PageNumber > 0
                                                    ? syncData.PageNumber
                                                    : 0;

                if (!string.IsNullOrWhiteSpace(syncData.PagingCookie))
                {
                    viewFilter.PageInfo.PagingCookie = syncData.PagingCookie;
                }

                // sync batch of 1000 contacts to CM list as subscribers
                EntityCollection contacts = orgService.RetrieveMultiple(viewFilter);                               

                syncData.PagingCookie = contacts.PagingCookie;
                syncData.PageNumber++;

                var subscribers = GenerateSubscribersList(contacts, primaryEmail);                

                BulkImportResults importResults = sub.Import(subscribers, 
                    false, // resubscribe
                    false, // queueSubscriptionBasedAutoResponders
                    false); // restartSubscriptionBasedAutoResponders
                
                if (importResults.FailureDetails.Count > 0)
                {
                    trace.Trace("{0} errors occured on page {1}.", importResults.FailureDetails.Count, viewFilter.PageInfo.PageNumber);

                    if (syncData.BulkSyncErrors == null)
                    {
                        syncData.BulkSyncErrors = new List<BulkSyncError>();
                    }

                    // log the errors back into bulk sync data
                    foreach (var failure in importResults.FailureDetails)
                    {
                        syncData.BulkSyncErrors.Add(new BulkSyncError(failure.Code, failure.Message, failure.EmailAddress));
                    }
                }

                if (!contacts.MoreRecords)
                {
                    syncData.PageNumber = 0;
                    syncData.PagingCookie = string.Empty;
                    break;
                }
            }
            while (timer.ElapsedMilliseconds >= 90000);

            trace.Trace("Saving bulk data.");
            string bulkData = JsonConvert.SerializeObject(syncData);
            config.BulkSyncData = bulkData;            
            configService.SaveConfig(config);

            return syncData.PageNumber <= 0; // if we're done return true
        }

        private List<SubscriberDetail> GenerateSubscribersList(EntityCollection contacts, string primaryEmail)
        {
            var subscribers = new List<SubscriberDetail>();
            MetadataHelper mdh = new MetadataHelper(orgService, trace);
            trace.Trace("Generating Subscriber List");

            trace.Trace("First contact contains primary email? {0}", contacts.Entities[0].Attributes.Contains(primaryEmail));

            foreach (Entity contact in contacts.Entities.Where(c => 
                                            c.Attributes.Contains(primaryEmail) && 
                                            !string.IsNullOrWhiteSpace(c[primaryEmail].ToString())))
            {
                trace.Trace("Contact {0} contains primary email? {1}", contacts.Entities.IndexOf(contact), contact.Attributes.Contains(primaryEmail));
                trace.Trace("Contact contains fullname? {0}", contact.Attributes.Contains("fullname"));
                trace.Trace("Contact = {0}", contact["fullname"].ToString());

                // remove the primary email field, it's sent as a separate param and we don't want duplicate fields
                var email = contact[primaryEmail].ToString();
                var name = contact["fullname"].ToString();

                // check to make sure this contact isn't duplicated within the filter for the config
                if (!config.SyncDuplicateEmails && SharedLogic.CheckEmailIsDuplicate(orgService, config, primaryEmail, email))
                {
                    continue;
                }

                // TODO: optimize, can cache the prettified schema names after the first contact and then just use the mapping
                var fields = SharedLogic.ContactAttributesToSubscriberFields(orgService, trace, contact, contact.Attributes.Keys);
                fields = SharedLogic.PrettifySchemaNames(mdh, fields);

                subscribers.Add(new SubscriberDetail {
                    EmailAddress = email,
                    Name = name,
                    CustomFields = fields
                });

                trace.Trace("Contact added.");
            }

            return subscribers;
        }
    }
}
