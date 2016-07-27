using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using createsend_dotnet;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;

namespace Campmon.Dynamics.Logic
{
    public class SharedLogic
    {
        public static string GetPrimaryEmailField(OptionSetValue val)
        {
            int value = val.Value;

            // get corresponding email field from contact entity based on value of optionset from config
            if (Enum.IsDefined(typeof(SubscriberEmailValues), value))
            {
                SubscriberEmailValues emailField = (SubscriberEmailValues)value;
                return emailField.ToString().ToLower();
            }

            return string.Empty;
        }

        public static List<SubscriberCustomField> ContactAttributesToSubscriberFields(IOrganizationService orgService, ITracingService tracer, Entity contact, ICollection<String> attributes)
        {
            MetadataHelper metadataHelper = new MetadataHelper(orgService, tracer);

            var fields = new List<SubscriberCustomField>();
            foreach (var field in attributes)
            {
                if (!contact.Attributes.Contains(field))
                {
                    continue;
                }

                if (contact[field] is EntityReference)
                {
                    // To transform Lookup and Option Set fields, use the text label and send as text
                    var refr = (EntityReference)contact[field];
                    var displayName = refr.Name;

                    // if name is empty, retrieve the entity and get it's primary attribute
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        var entity = orgService.Retrieve(refr.LogicalName, refr.Id, new ColumnSet(false));
                        displayName = entity.ToEntityReference().Name;
                    }

                    fields.Add(new SubscriberCustomField { Key = field, Value = displayName });
                }
                else if (contact[field] is OptionSetValue)
                {
                    var optionValue = (OptionSetValue)contact[field];
                    var optionLabel = metadataHelper.GetOptionSetValueLabel("contact", field, optionValue.Value);
                    fields.Add(new SubscriberCustomField { Key = field, Value = optionLabel });
                }
                else if (contact[field] is DateTime)
                {
                    // To transform date fields, send as date
                    var date = (DateTime)contact[field];
                    fields.Add(new SubscriberCustomField { Key = field, Value = date.ToString("yyyy/mm/dd") });
                }
                else if (contact[field] is Money)
                {
                    var mon = (Money)contact[field];
                    fields.Add(new SubscriberCustomField { Key = field, Value = mon.Value.ToString() });
                }
                else if (IsNumeric(contact[field]))
                {
                    // To transform numeric fields, send as number
                    fields.Add(new SubscriberCustomField { Key = field, Value = contact[field].ToString() });
                }
                else
                {
                    // For any other fields, send as text
                    fields.Add(new SubscriberCustomField { Key = field, Value = contact[field].ToString() });
                }
            }

            // convert schema names to display names to be cleaner for campaign monitor
            //fields = PrettifySchemaNames(metadataHelper, fields);

            return fields;
        }

        public static bool CheckEmailIsDuplicate(IOrganizationService orgService, string primaryEmailField, string email)
        {
            QueryExpression query = new QueryExpression("contact");
            query.Criteria.AddCondition(new ConditionExpression(primaryEmailField, ConditionOperator.Equal, email));
            query.ColumnSet.AddColumn("contactid");
            query.TopCount = 2;

            return orgService.RetrieveMultiple(query).TotalRecordCount > 1;
        }

        public static QueryExpression GetConfigFilterQuery(IOrganizationService orgService, Guid viewId)
        {
            ColumnSet cols = new ColumnSet("fetchxml");
            Entity view = orgService.Retrieve("view", viewId, cols);

            if (view == null || !view.Contains("fetchxml"))
            {
                return null;
            }

            var fetchRequest = new FetchXmlToQueryExpressionRequest
            {
                FetchXml = view["fetchxml"].ToString()
            };

            var queryResponse = (FetchXmlToQueryExpressionResponse)orgService.Execute(fetchRequest);
            var query = queryResponse.Query;

            return query;
        }

        public static AuthenticationDetails GetAuthentication(CampaignMonitorConfiguration config)
        {
            return new ApiKeyAuthenticationDetails(config.AccessToken);
        }

        private static bool IsNumeric(object Expression)
        {
            double retNum;
            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }

        private static List<SubscriberCustomField> PrettifySchemaNames(MetadataHelper metadataHelper, List<SubscriberCustomField> fields)
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field            
            AttributeMetadata[] attributes = metadataHelper.GetEntityAttributes("contact");

            foreach (var field in fields)
            {
                var displayName = (from x in attributes where x.LogicalName == field.Key select x.DisplayName).FirstOrDefault();
                if (displayName.UserLocalizedLabel != null && displayName.UserLocalizedLabel.Label != null)
                {
                    field.Key = displayName.UserLocalizedLabel.Label.ToString();
                }
            }

            return fields;
        }
    }
}
