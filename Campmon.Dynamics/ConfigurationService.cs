using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Campmon.Dynamics
{
    public class ConfigurationService
    {
        private IOrganizationService orgService;
        private ITracingService tracer;

        public ConfigurationService(IOrganizationService organizationService, ITracingService trace)
        {
            if (organizationService == null)
                throw new ArgumentNullException("organizationService");

            orgService = organizationService;
            tracer = trace;
        }

        public string GetAccessToken()
        {
            var query = new QueryExpression("campmon_configuration");
            query.TopCount = 1;
            query.ColumnSet = new ColumnSet("campmon_accesstoken");
            var result = orgService.RetrieveMultiple(query);

            if (!result.Entities.Any())
            {
                return string.Empty;
            }

            return result.Entities.First().GetAttributeValue<string>("campmon_accesstoken");
        }
        public CampaignMonitorConfiguration VerifyAndLoadConfig()
        {
            var query = new QueryExpression("campmon_configuration");
            query.TopCount = 1;
            query.ColumnSet = new ColumnSet("campmon_accesstoken", "campmon_bulksyncdata", "campmon_bulksyncinprogress",
                "campmon_clientid", "campmon_clientname", "campmon_listid", "campmon_listname", "campmon_setuperror",
                "campmon_syncduplicateemails", "campmon_syncfields", "campmon_syncviewid", "campmon_syncviewname",
                "campmon_subscriberemail");

            var result = orgService.RetrieveMultiple(query);

            if (!result.Entities.Any())
            {
                return null;
            }

            var configEntity = result.Entities.First();

            var config = new CampaignMonitorConfiguration
            {
                AccessToken = configEntity.GetAttributeValue<string>("campmon_accesstoken"),
                BulkSyncData = configEntity.GetAttributeValue<string>("campmon_bulksyncdata"),
                ClientId = configEntity.GetAttributeValue<string>("campmon_clientid"),
                ClientName = configEntity.GetAttributeValue<string>("campmon_clientname"),
                ListId = configEntity.GetAttributeValue<string>("campmon_listid"),
                ListName = configEntity.GetAttributeValue<string>("campmon_listname"),
                SetUpError = configEntity.GetAttributeValue<string>("campmon_setuperror"),
                SyncDuplicateEmails = configEntity.GetAttributeValue<bool>("campmon_syncduplicateemails"),
                SyncFields = configEntity.Contains("campmon_syncfields") && !string.IsNullOrWhiteSpace(configEntity.GetAttributeValue<string>("campmon_syncfields"))
                    ? configEntity.GetAttributeValue<string>("campmon_syncfields").Split(',')
                    : Enumerable.Empty<string>(),
                SyncViewId = configEntity.Contains("campmon_syncviewid")
                    ? Guid.Parse(configEntity.GetAttributeValue<string>("campmon_syncviewid"))
                    : Guid.Empty,
                SyncViewName = configEntity.GetAttributeValue<string>("campmon_syncviewname"),
                SubscriberEmail = configEntity.Contains("campmon_subscriberemail")
                    ? configEntity.GetAttributeValue<OptionSetValue>("campmon_subscriberemail")
                    : new OptionSetValue(-1)
            };

            // check if ClientID, AccessToken, SusbcriberEmail fields are correct, and if not then return null
            if (string.IsNullOrWhiteSpace(config.AccessToken) || string.IsNullOrWhiteSpace(config.ClientId) 
                || config.SubscriberEmail.Value < 0)
            {
                tracer.Trace("Configuration record does not contain ClientID, AccessToken, or SubscriberEmail");
                return null;
            }

            return config;
        }

        public void SaveConfig(CampaignMonitorConfiguration config)
        {
            throw new NotImplementedException();
        }

    }
}
