using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Newtonsoft.Json;
using createsend_dotnet;

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
            CampaignMonitorConfiguration campaignMonitorConfig = configService.LoadConfig();
            if (campaignMonitorConfig == null ||
                    campaignMonitorConfig.AccessToken == null || campaignMonitorConfig.ClientId == null)
            {
                _tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            // Check that the email field in campmon_emailaddress has data in the contact post image. If it is empty, exit the plugin.
            if (campaignMonitorConfig.SubscriberEmail.Value < 0)
            {
                return;
            }

            string emailField = GetEmailFieldFromOptionSet(campaignMonitorConfig.SubscriberEmail.Value);                                  
            if (string.IsNullOrWhiteSpace(emailField) || 
                    !postImage.Contains(emailField) || string.IsNullOrWhiteSpace(postImage[emailField].ToString()))
            {
                _tracer.Trace("The email field to sync is missing or contains invalid data.");
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
            
            syncMessage["campmon_name"] = isUpdate ? "update" : "create";
            syncMessage["campmon_data"] = syncData;
            syncMessage["campmon_email"] = emailField;
            _orgService.Create(syncMessage);
        }

        private string GetEmailFieldFromOptionSet(int value)
        {
            // get corresponding email field from contact entity based on value of optionset from config
            if (Enum.IsDefined(typeof(SubscriberEmailValues), value))
            {
                SubscriberEmailValues emailField = (SubscriberEmailValues)value;
                return emailField.ToString().ToLower();
            }
            
            return string.Empty;            
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
            //      It would be best to serialize into JSON array of objects from the CM API
            //      - This will also be a lot easier when deserializing/editing in SendMessagePlugin

            var fieldList = new List<SubscriberCustomField>();         

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
                    fieldList.Add(new SubscriberCustomField { Key = field, Value = refr.Name });
                }
                else if (target[field].GetType() == typeof(OptionSetValue))
                {
                    var opst = (OptionSetValue)target[field];
                    fieldList.Add(new SubscriberCustomField { Key = field, Value = opst.ToString() });
                }
                else if (target[field].GetType() == typeof(DateTime))
                {
                    // To transform date fields, send as date
                    var date = (DateTime)target[field];
                    fieldList.Add(new SubscriberCustomField { Key = field, Value = date.ToString("yyyy/mm/dd") });
                }
                else if (IsNumeric(target[field]))
                {
                    // To transform numeric fields, send as number
                    fieldList.Add(new SubscriberCustomField { Key = field, Value = target[field].ToString() });                    
                }
                else
                {
                    // For any other fields, send as text
                    fieldList.Add(new SubscriberCustomField { Key = field, Value = target[field].ToString() });
                }
            }

            return fieldList.Count > 0 
                ? JsonConvert.SerializeObject(fieldList) 
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