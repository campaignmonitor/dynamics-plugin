using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Diagnostics;
using Campmon.Dynamics.Logic;
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

            BulkSyncData syncData = config.BulkSyncData != null
                                        ? syncData = JsonConvert.DeserializeObject<BulkSyncData>(config.BulkSyncData)
                                        : new BulkSyncData();

            string primaryEmail = SharedLogic.GetPrimaryEmailField(config.SubscriberEmail);

            QueryExpression viewFilter = GetBulkSyncFilter(config, syncData, primaryEmail);            

            var auth = Authenticator.GetAuthentication(config);
            var sub = new Subscriber(auth, config.ListId);
            var mdh = new MetadataHelper(orgService, trace);

            trace.Trace("Beginning the sync process.");
                                
            do
            {
                viewFilter.PageInfo.PageNumber = syncData.PageNumber > 1
                                                    ? syncData.PageNumber
                                                    : 1;

                if (!string.IsNullOrWhiteSpace(syncData.PagingCookie))
                {
                    viewFilter.PageInfo.PagingCookie = syncData.PagingCookie;
                }

                // sync batch of 1000 contacts to CM list as subscribers
                EntityCollection contacts = orgService.RetrieveMultiple(viewFilter);                               

                syncData.PagingCookie = contacts.PagingCookie;
                syncData.PageNumber++;

                IEnumerable<Entity> invalidEmail = contacts.Entities.Where(e => !e.Attributes.Contains(primaryEmail) || string.IsNullOrWhiteSpace(e[primaryEmail].ToString()));
                syncData.NumberInvalidEmails += invalidEmail.Count();

                var subscribers = GenerateSubscribersList(contacts.Entities.Except(invalidEmail), primaryEmail, mdh);                
                
                BulkImportResults importResults = sub.Import(subscribers, 
                    false, // resubscribe
                    false, // queueSubscriptionBasedAutoResponders
                    false); // restartSubscriptionBasedAutoResponders
                
                if (importResults.FailureDetails.Count > 0)
                {
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

                trace.Trace("Page: {0}", syncData.PageNumber);
                trace.Trace("More Records? {0}", contacts.MoreRecords);

                if (!contacts.MoreRecords)
                {
                    trace.Trace("No more records, clearing the sync data.");
                    syncData.PageNumber = 1;
                    syncData.PagingCookie = string.Empty;
                    syncData.UpdatedFields = null;                    

                    syncData.BulkSyncErrors.Clear();
                    syncData.NumberInvalidEmails = 0;                   
                    break;
                }
            }
            while (timer.ElapsedMilliseconds <= 90000);

            trace.Trace("Saving bulk data.");
            string bulkData = JsonConvert.SerializeObject(syncData);
            config.BulkSyncData = bulkData;
            config.BulkSyncInProgress = syncData.PageNumber > 1;
            configService.SaveConfig(config);

            return syncData.PageNumber <= 1; // if we're done return true
        }

        private QueryExpression GetBulkSyncFilter(CampaignMonitorConfiguration config, BulkSyncData syncData, string primaryEmail)
        {
            // retrieve contacts based on the filter, grabbing the columns specified either 
            // in the fields to sync (on config entity) or fields specified in the bulkdata sync fields
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
            viewFilter.PageInfo.Count = BATCH_AMOUNT;
            viewFilter.PageInfo.ReturnTotalRecordCount = true;

            return viewFilter;
        }

        private List<SubscriberDetail> GenerateSubscribersList(IEnumerable<Entity> contacts, string primaryEmail, MetadataHelper mdh)
        {

            trace.Trace("Generating Subscriber List");
            var subscribers = new List<SubscriberDetail>();            

            foreach (Entity contact in contacts)
            {

                // remove the primary email field, it's sent as a separate param and we don't want duplicate fields
                var email = contact[primaryEmail].ToString();
                var name = contact["fullname"].ToString();

                // check to make sure this contact isn't duplicated within the filter for the config
                if (!config.SyncDuplicateEmails && SharedLogic.CheckEmailIsDuplicate(orgService, config, primaryEmail, email))
                {
                    continue;
                }
                
                var fields = SharedLogic.ContactAttributesToSubscriberFields(orgService, trace, contact, contact.Attributes.Keys);
                fields = SharedLogic.PrettifySchemaNames(mdh, fields);

                subscribers.Add(new SubscriberDetail {
                    EmailAddress = email,
                    Name = name,
                    CustomFields = fields
                });
            }

            return subscribers;
        }
    }
}
