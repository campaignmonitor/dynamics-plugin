using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;

namespace Campmon.Dynamics
{
    public class Authenticator
    {
        public static AuthenticationDetails GetAuthentication(CampaignMonitorConfiguration config)
        {
            return new OAuthAuthenticationDetails(config.AccessToken, config.RefreshToken);
        }
    }
}
