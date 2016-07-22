using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Campmon.Dynamics;
using Microsoft.Crm.Sdk.Messages;

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
            // 3. Check that the email field in campmon_emailaddress has data in the contact post image. If it is empty, exit the plugin.
            if (target == null || postImage == null || postImage["campmon_emailaddress"] == null)
            {
                return;
            }

            // 1. Retrieve the Campaign Monitor Configuration record. If it does not exist or is missing access token or client id, exit the plugin.
            ConfigurationService configService = new ConfigurationService(_orgService);
            CampaignMonitorConfiguration campaignMonitorConfig = configService.LoadConfig();            
            if (campaignMonitorConfig == null ||
                    campaignMonitorConfig.AccessToken == null || campaignMonitorConfig.ClientId == null)
            {
                _tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            // 2. Retrieve the view specified in the campmon_syncviewid field of the configuration record.
            var filterView = RetrieveSyncFilter(campaignMonitorConfig.SyncViewId);
            var filterFetchXml = filterView["fetchxml"].ToString();

            // 4.Modify the sync view fetch query to include a filter condition for the current contact id.Execute the modified query and check if the contact is returned.If it is, exit the plugin.
            if (!TestContactFitsFilter(filterFetchXml, (Guid)target["contactid"]))
            {
                _tracer.Trace("Contact does not fit the filter.");
                return;
            }

            // 5. If this is an update operation, check that the plugin target has modified attributes that are included in the campmon_syncfields data. If there are not any sync fields in the target, exit the plugin.

            // this is ugly; it should work to just test the return string of serialize method and if == {} then no fields are updated and don't sync?
/*          if (isUpdate)
            {
                var st = new List<String>();
                // add fields to a collection
                foreach (var field in campaignMonitorConfig.SyncFields)
                {
                    if (target.Attributes.Contains(field))
                    {
                        st.Add(field);
                    }
                }

                if (st.Count <= 0) return;
            }*/

            /*
                6. Create a campmon_message record with the following data:
                    • campmon_sdkmessage = Plugin message (create or update)
                    • campmon_data = JSON serialized sync data
            */
            var syncMessage = new Entity("campmon_message");
            var syncData = SerializeDataToSync(target, campaignMonitorConfig);

            if (syncData == "{}") { return; }

            syncMessage["campmon_sdkmessage"] = isUpdate ? "update" : "create";
            syncMessage["campmon_data"] = syncData;
            _orgService.Create(syncMessage);
        }

        internal Entity RetrieveSyncFilter(Guid viewID)
        {
            QueryExpression syncFilterQuery = new QueryExpression("view");
            ConditionExpression queryIdCondition = new ConditionExpression("savedqueryid", ConditionOperator.Equal, viewID);
            syncFilterQuery.Criteria.AddCondition(queryIdCondition);

            syncFilterQuery.ColumnSet.AddColumn("fetchxml");

            var syncFilter = _orgService.RetrieveMultiple(syncFilterQuery).Entities.FirstOrDefault();
            return syncFilter;
        }

        internal bool TestContactFitsFilter(string fetchXml, Guid contactID)
        {
            var conversionRequest = new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml };
            var conversionResponse = (FetchXmlToQueryExpressionResponse)_orgService.Execute(conversionRequest);

            QueryExpression filterQuery = conversionResponse.Query;
            ConditionExpression contactCondition = new ConditionExpression("contactid", ConditionOperator.Equal, contactID);
            filterQuery.Criteria.AddCondition(contactCondition);

            var contacts = _orgService.RetrieveMultiple(filterQuery).Entities;
            return contacts.Count >= 1;
        }

        internal string SerializeDataToSync(Entity target, CampaignMonitorConfiguration config)
        {
            /*
                   7. To serialize the sync data, create a single object with each field schema name as the property with its associated value. Use the following rules to convert data:
                       • To transform Lookup and Option Set fields, use the text label and send as text
                       • To transform date fields, send as date
                       • To transform numeric fields, send as a number
                       • For any other fields, send as text
               */
            var syncData = "{";
            foreach (var field in config.SyncFields)
            {
                if (!target.Attributes.Contains(field))
                {
                    continue;
                }

                if (target.Attributes[field].GetType() == typeof(EntityReference))
                {
                    var refr = (EntityReference)target.Attributes[field];
                    syncData += string.Format("{0}:\"{1}\",", field, refr.Name);
                }
                else if (target.Attributes[field].GetType() == typeof(OptionSetValue))
                {
                    var opst = (OptionSetValue)target.Attributes[field];
                    syncData += string.Format("{0}:\"{1}\",", field, opst.ToString());
                }
                else if (target.Attributes[field].GetType() == typeof(DateTime))
                {
                    var date = (DateTime)target.Attributes[field];
                    syncData += string.Format("{0}:{1},", field, date.ToLongDateString()); //?
                }
                else if (IsNumeric(target.Attributes[field]))
                {
                    syncData += string.Format("{0}:{1},", field, target.Attributes[field].ToString());
                }
                else
                {
                    // quotes around {1} to signify string val
                    syncData += string.Format("{0}:\"{1}\",", field, target.Attributes[field].ToString());
                }
            }

            syncData += "}";

            return syncData;
        }

        public static bool IsNumeric(object Expression)
        {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }
    }    
}