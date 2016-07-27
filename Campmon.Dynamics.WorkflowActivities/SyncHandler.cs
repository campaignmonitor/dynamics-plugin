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

            var configService = new ConfigurationService(orgService);
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

            // we need the correct email for the subscription, and potentially the fullname(?)
            // need some sort of name field otherwise it will be blank on CM
            if (!viewFilter.ColumnSet.Columns.Contains("fullname"))
            {
                viewFilter.ColumnSet.Columns.Add("fullname");
            }

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
                    var email = contact.Attributes[primaryEmail].ToString();
                    var name = contact.Attributes["fullname"].ToString();
                    contact.Attributes.Remove(primaryEmail);
                    contact.Attributes.Remove("fullname");


                    SubscriberDetail sd = new SubscriberDetail();
                    
                    var fields = SharedLogic.ContactAttributesToSubscriberFields(orgService, contact, contact.Attributes.Keys);
                    subscribers.Add(new SubscriberDetail(email, name, fields));                    
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
