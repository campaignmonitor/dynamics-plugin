using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using createsend_dotnet;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
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

        public static List<SubscriberCustomField> ContactAttributesToSubscriberFields(IOrganizationService orgService, Entity contact, ICollection<String> attributes)
        {            
            attributes = PrettifySchemaNames(orgService, attributes);

            var fields = new List<SubscriberCustomField>();
            foreach (var field in attributes)
            {
                if (!contact.Attributes.Contains(field))
                {
                    continue;
                }

                if (contact[field].GetType() == typeof(EntityReference))
                {
                    // To transform Lookup and Option Set fields, use the text label and send as text
                    var refr = (EntityReference)contact[field];
                    fields.Add(new SubscriberCustomField { Key = field, Value = refr.Name });
                }
                else if (contact[field].GetType() == typeof(OptionSetValue))
                {
                    var opst = (OptionSetValue) contact[field];
                    fields.Add(new SubscriberCustomField { Key = field, Value = opst.ToString() });
                }
                else if (contact[field].GetType() == typeof(DateTime))
                {
                    // To transform date fields, send as date
                    var date = (DateTime)contact[field];
                    fields.Add(new SubscriberCustomField { Key = field, Value = date.ToString("yyyy/mm/dd") });
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

        private static List<string> PrettifySchemaNames(IOrganizationService orgService, ICollection<String> fields)
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field
            RetrieveEntityRequest getEntityMetadataRequest = new RetrieveEntityRequest
            {
                LogicalName = "contact",
                RetrieveAsIfPublished = true
            };
            RetrieveEntityResponse entityMetaData = (RetrieveEntityResponse)orgService.Execute(getEntityMetadataRequest);

            List<string> prettyFields = new List<string>();
            foreach (var field in fields)
            {
                var displayName = from x in entityMetaData.EntityMetadata.Attributes
                                  where x.LogicalName == field
                                  select x.DisplayName.ToString();

                if (displayName.Any())
                {
                    prettyFields.Add(displayName.First().ToString());
                }
            }

            return prettyFields;
        }
    }
}
