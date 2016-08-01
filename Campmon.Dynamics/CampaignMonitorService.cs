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
        private ConfigurationService _config { get; set; }

        public CampaignMonitorService(ConfigurationService config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;
        }

        //TODO: Add methods used across operations here and cache the service to provide quicker turnaround
        public General GetAuthGeneral()
        {
            var auth = new ApiKeyAuthenticationDetails(_config.GetAccessToken());
            return new General(auth);
        }

        public string GetAuthToken()
        {
            return _config.GetAccessToken();
        }
 
    }
}
