using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Diagnostics;
using Campmon.Dynamics;
using Campmon.Dynamics.Logic;
using Newtonsoft.Json;
using createsend_dotnet;
using System.Collections.Generic;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class SyncHandler
    {
        private IOrganizationService orgService;
        private ITracingService trace;
        private Stopwatch timer;
        private CampaignMonitorConfiguration config;

        public SyncHandler(IOrganizationService organizationService, ITracingService tracingService, Stopwatch executionTimer)
        {
            orgService = organizationService;
            trace = tracingService;
            timer = executionTimer;

            var configService = new ConfigurationService(orgService, trace);
            trace.Trace("Loading configuration.");
            config = configService.VerifyAndLoadConfig();
        }

        public bool Run()
        {
            trace.Trace("Deserializing bulk sync data.");
            var syncData = JsonConvert.DeserializeObject<BulkSyncData>(config.BulkSyncData);

            // get view and modify.
            // if syncData.UpdatedFields contains data, only sync those fields.

            var primaryEmail = SharedLogic.GetPrimaryEmailField(config.SubscriberEmail);

            QueryExpression viewFilter = SharedLogic.GetConfigFilterQuery(orgService, config.SyncViewId);
            if (syncData.UpdatedFields.Length > 0)
            {
                viewFilter.ColumnSet.Columns.Clear();
                syncData.UpdatedFields.ToList().ForEach(x => viewFilter.ColumnSet.Columns.Add(x));
            }

            // we need the correct email for the subscription
            if (!viewFilter.ColumnSet.Columns.Contains(primaryEmail))
            {
                viewFilter.ColumnSet.Columns.Add(primaryEmail);
            }

            if (syncData.PageNumber >= 0)
            {
                viewFilter.PageInfo.PageNumber = syncData.PageNumber;
            }

            if (!string.IsNullOrWhiteSpace(syncData.PagingCookie))
            {
                viewFilter.PageInfo.PagingCookie = syncData.PagingCookie;
            }

            viewFilter.AddOrder("modifiedon", OrderType.Ascending);
            viewFilter.TopCount = 1000;

            var auth = SharedLogic.GetAuthentication(config);
            var sub = new Subscriber(auth, config.ListId);
            List<SubscriberDetail> subscribers = new List<SubscriberDetail>();

            do
            {
                // sync batch of 1000 contacts to CM list as subscribers
                EntityCollection contacts = orgService.RetrieveMultiple(viewFilter);
                foreach (Entity contact in contacts.Entities)
                {
                    // remove the primary email field as it's sent as a separate param and don't want duplicates on CM
                    var email = contact.Attributes[primaryEmail].ToString();
                    contact.Attributes.Remove(primaryEmail);

                    string name = null;
                    if (contact.Contains("fullname"))
                    {
                        name = contact["fullname"].ToString();
                        contact.Attributes.Remove("fullname");
                    }


                    var fields = SharedLogic.ContactAttributesToSubscriberFields(orgService, contact, contact.Attributes.Keys);
                    subscribers.Add(new SubscriberDetail(email, 
                                                         string.IsNullOrWhiteSpace(name) 
                                                            ? string.Empty 
                                                            : name,
                                                         fields));
                }

                BulkImportResults importResults = sub.Import(subscribers, false);
                //if (importResults.FailureDetails.Count > 0)
                //{
                //    // some failures occurred.
                //    foreach (var failure in importResults.FailureDetails)
                //    {
                //        // do something here?
                //        //failure.Code
                //        //failure.EmailAddress
                //        //failure.Message
                //    }
                //}
                
                viewFilter.PageInfo.PagingCookie = syncData.PagingCookie = contacts.PagingCookie;
                viewFilter.PageInfo.PageNumber = syncData.PageNumber = syncData.PageNumber + 1; // ?? contacts.PageNumber isn't a thing
            }
            while (timer.ElapsedMilliseconds >= 90000);

            return true;
            throw new NotImplementedException();
        }        
    }
}
