using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics
{
    public class ConfigurationService
    {
        private IOrganizationService orgService;

        public ConfigurationService(IOrganizationService organizationService)
        {
            if (organizationService == null)
                throw new ArgumentNullException("organizationService");

            orgService = organizationService;
        }

        public CampaignMonitorConfiguration LoadConfig()
        {
            var query = new QueryExpression("campmon_configuration");
            query.TopCount = 1;
            query.ColumnSet = new ColumnSet("campmon_accesstoken", "campmon_bulksyncdata", "campmon_bulksyncinprogress",
                "campmon_clientid", "campmon_clientname", "campmon_listid", "campmon_listname", "campmon_setuperror",
                "campmon_syncduplicateemails", "campmon_syncfields", "campmon_syncviewid", "campmon_syncviewname");
            
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
                SyncFields = configEntity.Contains("campmon_syncfields")
                    ? configEntity.GetAttributeValue<string>("campmon_syncfields").Split(',')
                    : Enumerable.Empty<string>(),
                SyncViewId = configEntity.Contains("campmon_syncviewid")
                    ? Guid.Parse(configEntity.GetAttributeValue<string>("campmon_syncviewid"))
                    : Guid.Empty,
                SyncViewName = configEntity.GetAttributeValue<string>("campmon_syncviewname")
            };

            if (configEntity.Contains("campmon_bulksyncinprogress"))
            {
                var value = configEntity.GetAttributeValue<OptionSetValue>("campmon_bulksyncinprogress").Value;
            }

            if (configEntity.Contains("campmon_syncviewid"))
            {

            }

            config.SyncDuplicateEmails = false;

            return config;
        }

        public void SaveConfig(CampaignMonitorConfiguration config)
        {
            throw new NotImplementedException();
        }

    }
}
