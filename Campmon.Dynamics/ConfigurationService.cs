using Microsoft.Xrm.Sdk;
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
            throw new NotImplementedException();
        }

        public void SaveConfig(CampaignMonitorConfiguration config)
        {
            throw new NotImplementedException();
        }

    }
}
