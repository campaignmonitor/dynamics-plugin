using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using createsend_dotnet;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
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

        public static List<SubscriberCustomField> ContactAttributesToSubscriberFields(IOrganizationService orgService, Entity contact, ICollection<String> attributes)
        {            
            

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
                    var optionValue = (OptionSetValue) contact[field];
                    var optionLabel = GetOptionSetValueLabel(orgService, "contact", field, optionValue.Value);
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
            //fields = PrettifySchemaNames(orgService, fields);

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

        private static List<SubscriberCustomField> PrettifySchemaNames(IOrganizationService orgService, List<SubscriberCustomField> fields)
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field
            RetrieveEntityRequest getEntityMetadataRequest = new RetrieveEntityRequest
            {
                LogicalName = "contact",
                RetrieveAsIfPublished = true,
                EntityFilters = EntityFilters.Attributes
            };
            RetrieveEntityResponse entityMetaData = (RetrieveEntityResponse)orgService.Execute(getEntityMetadataRequest);
            
            foreach (var field in fields)
            {
                var displayName = from x in entityMetaData.EntityMetadata.Attributes
                                  where x.LogicalName == field.Key
                                  select x.DisplayName;

                if (displayName.Any())
                {
                    var disp = displayName.First();
                    if (disp.UserLocalizedLabel != null && disp.UserLocalizedLabel.Label != null)
                    {
                        field.Key = disp.UserLocalizedLabel.Label.ToString();
                    }
                }
            }

            return fields;
        }

        private static string GetOptionSetValueLabel(IOrganizationService orgService, string entityName, string fieldName, int optionSetValue)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = fieldName,
                RetrieveAsIfPublished = true
            };

            var attResponse = (RetrieveAttributeResponse)orgService.Execute(request);
            var attMetadata = (EnumAttributeMetadata)attResponse.AttributeMetadata;

            var optionMetadata = attMetadata.OptionSet.Options.Where(x => x.Value == optionSetValue).FirstOrDefault();
            
            if (optionMetadata != null && optionMetadata.Label != null && optionMetadata.Label.UserLocalizedLabel != null)
            {
                return optionMetadata.Label.UserLocalizedLabel.Label;
            }

            return string.Empty;
        }
    }
}
