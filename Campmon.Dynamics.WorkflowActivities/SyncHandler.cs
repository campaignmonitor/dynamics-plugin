using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
            var syncData = JsonConvert.DeserializeObject<BulkSyncData>(config.BulkSyncData);
            var primaryEmail = SharedLogic.GetPrimaryEmailField(config.SubscriberEmail);

            // retrieve contacts based on the filter, grabbing the columns specified either in the fields to sync (on config entity)
            // or fields specified in the bulkdata sync fields
            QueryExpression viewFilter = SharedLogic.GetConfigFilterQuery(orgService, config.SyncViewId);
            viewFilter.ColumnSet.Columns.Clear();
            if (syncData.UpdatedFields.Length > 0)
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

            foreach (Entity contact in contacts.Entities.Where(x => !string.IsNullOrWhiteSpace(x[primaryEmail].ToString())))
            {
                // remove the primary email field, it's sent as a separate param and we don't want duplicate fields
                var email = contact.Attributes[primaryEmail].ToString();
                var name = contact["fullname"].ToString();

                // temporary; I could technically just look at contacts collection for duplicates correct?
                // basically since those are the ones that match the filter to be sent.
                // this will be way slower since it needs to do retrieve multiples for EVERY contact coming in.
                if (!config.SyncDuplicateEmails && SharedLogic.CheckEmailIsDuplicate(orgService, primaryEmail, email))
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
