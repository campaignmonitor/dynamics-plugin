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
            trace.Trace("Constructing MetadataHelper.");
            orgService = organizationService;
            tracer = trace;
            metadata = new MetadataService(orgService);

            primaryAttributes = new Dictionary<string, string>();
            optionSetLabels = new Dictionary<string, string>();
        }

        public string GetOptionSetValueLabel(string entityLogicalName, string attribute, int optionSetValue)
        {
            var dictKey = string.Format("{0}{1}", entityLogicalName, attribute);
            if (!optionSetLabels.ContainsKey(dictKey))
            {
                var label = metadata.GetStringValueFromPicklistInt(entityLogicalName, attribute, optionSetValue);
                if (string.IsNullOrEmpty(label))
                {
                    tracer.Trace("Invalid OptionSet value");
                }
                optionSetLabels[dictKey] = label;
            }

            return primaryAttributes[entityLogicalName];
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
