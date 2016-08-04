using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics
{
    public class CampaignMonitorService
    {
        private CampaignMonitorConfiguration _config { get; set; }

        public CampaignMonitorService(CampaignMonitorConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;
        }

        //TODO: Add methods used across operations here and cache the service to provide quicker turnaround
        public General GetAuthGeneral()
        {
            var auth = new ApiKeyAuthenticationDetails(_config.AccessToken);
            return new General(auth);
        }
 
    }
}
