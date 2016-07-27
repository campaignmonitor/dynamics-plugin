using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Campmon.Dynamics
{
    class MetadataHelper
    {
        private IOrganizationService orgService;
        private ITracingService tracer;

        public MetadataHelper(IOrganizationService organizationService, ITracingService trace)
        {
            orgService = organizationService;
            tracer = trace;
        }

        public string GetOptionSetValueLabel(string entityLogicalName, string attribute, int optionSetValue)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attribute,
                RetrieveAsIfPublished = true
            };

            var attResponse = (RetrieveAttributeResponse)orgService.Execute(request);
            var attMetadata = (EnumAttributeMetadata)attResponse.AttributeMetadata;

            var optionMetadata = attMetadata.OptionSet.Options.Where(x => x.Value == optionSetValue).FirstOrDefault();

            if (optionMetadata != null && optionMetadata.Label != null && optionMetadata.Label.UserLocalizedLabel != null)
            {
                return optionMetadata.Label.UserLocalizedLabel.Label;
            }

            tracer.Trace("Invalid OptionSet value");
            return string.Empty;
        }

        public AttributeMetadata[] GetEntityAttributes(string entityLogicalName)
        {
            RetrieveEntityRequest getEntityMetadataRequest = new RetrieveEntityRequest
            {
                LogicalName = entityLogicalName,
                RetrieveAsIfPublished = true,
                EntityFilters = EntityFilters.Attributes
            };

            RetrieveEntityResponse entityMetaData = (RetrieveEntityResponse)orgService.Execute(getEntityMetadataRequest);           
            return entityMetaData.EntityMetadata.Attributes;
        }
    }
}
