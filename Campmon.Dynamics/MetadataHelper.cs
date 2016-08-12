using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using SonomaPartners.Crm.Toolkit;

namespace Campmon.Dynamics
{
    public class MetadataHelper
    {
        private IOrganizationService orgService;
        private ITracingService tracer;
        private MetadataService metadata;

        private IDictionary<string, string> primaryAttributes;
        private IDictionary<string, string> optionSetLabels;

        public MetadataHelper(IOrganizationService organizationService, ITracingService trace)
        {                        
            orgService = organizationService;
            tracer = trace;
            metadata = new MetadataService(orgService);

            primaryAttributes = new Dictionary<string, string>();
            optionSetLabels = new Dictionary<string, string>();
        }

        public string GetOptionSetValueLabel(string entityLogicalName, string attribute, int optionSetValue)
        {
            var dictKey = string.Format("{0}{1}{2}", entityLogicalName, attribute, optionSetValue.ToString());
            if (!optionSetLabels.ContainsKey(dictKey))
            {
                string label = string.Empty;

                if (attribute == "statuscode")
                {
                    label = metadata.GetStringValueFromStatusInt(entityLogicalName, attribute, optionSetValue);
                }
                else if (attribute == "statecode")
                {
                    label = optionSetValue == 0 ? "active" : "inactive";
                }
                else
                {
                    label = metadata.GetStringValueFromPicklistInt(entityLogicalName, attribute, optionSetValue);
                }

                if (string.IsNullOrEmpty(label))
                {
                    tracer.Trace("Invalid OptionSet value {0}:{1}", attribute, optionSetValue);
                }
                optionSetLabels[dictKey] = label;
            }

            return optionSetLabels[dictKey];            
        }

        public AttributeMetadata[] GetEntityAttributes(string entityLogicalName)
        {
            tracer.Trace("Getting entity attributes.");
            return metadata.RetrieveEntity(entityLogicalName, EntityFilters.Attributes).Attributes;
        }

        public string GetEntityPrimaryAttribute(string entityLogicalName)
        {
            if (!primaryAttributes.ContainsKey(entityLogicalName))
            {
                primaryAttributes[entityLogicalName] = metadata.GetPrimaryAttributeName(entityLogicalName);
            }

            return primaryAttributes[entityLogicalName];
        }
    }
}
