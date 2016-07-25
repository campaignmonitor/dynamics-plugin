using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Newtonsoft.Json;

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

            // Check that the email field in campmon_emailaddress has data in the contact post image. If it is empty, exit the plugin.
            //?

            /*
            if (!postImage.Contains("campmon_emailaddress") || postImage["campmon_emailaddress"] == null)
            {
                return; 
            } 
            */

            // Retrieve the Campaign Monitor Configuration record. If it does not exist or is missing access token or client id, exit the plugin.
            ConfigurationService configService = new ConfigurationService(_orgService);
            CampaignMonitorConfiguration campaignMonitorConfig = configService.LoadConfig();
            if (campaignMonitorConfig == null ||
                    campaignMonitorConfig.AccessToken == null || campaignMonitorConfig.ClientId == null)
            {
                _tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            // Retrieve the view specified in the campmon_syncviewid field of the configuration record.
            var filterView = RetrieveSyncFilter(campaignMonitorConfig.SyncViewId);
            var filterFetchXml = filterView["fetchxml"].ToString();

            // Modify the sync view fetch query to include a filter condition for the current contact id.Execute the modified query and check if the contact is returned.If it is, exit the plugin.
            if (!TestContactFitsFilter(filterFetchXml, (Guid)target["contactid"]))
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
            var syncData = SerializeDataToSync(target, campaignMonitorConfig);

            // If this is an update operation, check that the plugin target has modified attributes that are included in the campmon_syncfields data. If there are not any sync fields in the target, exit the plugin.
            if (string.IsNullOrWhiteSpace(syncData))
            {
                return;
            }

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
            //  To serialize the sync data, create a single object with each 
            //  field schema name as the property with its associated value.

            // Edit 7/25: Upon inspection of the createsend API:
            //      It would be best to serialize into an array of objects where each object has a Key and Value property
            //      - This will also be a lot easier when deserializing/editing in SendMessagePlugin

            var keyValueList = new List<KeyValuePair<string, object>>();

            foreach (var field in config.SyncFields)
            {
                if (!target.Attributes.Contains(field))
                {
                    continue;
                }

                if (target[field].GetType() == typeof(EntityReference))
                {
                    // To transform Lookup and Option Set fields, use the text label and send as text
                    var refr = (EntityReference)target[field];
                    keyValueList.Add(new KeyValuePair<string, object>(field, refr.Name));
                }
                else if (target[field].GetType() == typeof(OptionSetValue))
                {
                    var opst = (OptionSetValue)target[field];
                    keyValueList.Add(new KeyValuePair<string, object>(field, opst.ToString()));                    
                }
                else if (target[field].GetType() == typeof(DateTime))
                {
                    // To transform date fields, send as date
                    var date = (DateTime)target[field];
                    keyValueList.Add(new KeyValuePair<string, object>(field, date));                    
                }
                else if (IsNumeric(target[field]))
                {
                    // To transform numeric fields, send as number
                    keyValueList.Add(new KeyValuePair<string, object>(field, target[field])); 
                }
                else
                {
                    // For any other fields, send as text
                    keyValueList.Add(new KeyValuePair<string, object>(field, target[field].ToString()));                    
                }
            }

            return keyValueList.Count > 0 
                ? JsonConvert.SerializeObject(keyValueList) 
                : string.Empty;            
        }

        public static bool IsNumeric(object Expression)
        {
            double retNum;
            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }
    }
}