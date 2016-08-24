using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics
{
    public class Authenticator
    {
        public static AuthenticationDetails GetAuthentication(CampaignMonitorConfiguration config, IOrganizationService orgService)
        {
            var auth = new OAuthAuthenticationDetails(config.AccessToken, config.RefreshToken);
            if (config.TokenValidTo.HasValue && (config.TokenValidTo.Value.AddHours(12) > DateTime.UtcNow))
            {
                return auth;
            }
            else
            {
                // token has expired
                var general = new General(auth);
                var newToken = general.RefreshToken();

                var updatedAuth = new Entity("campmon_configuration", config.Id)
                {
                    Attributes =
                    {
                        {"campmon_accesstoken", newToken.access_token },
                        {"campmon_refreshtoken", newToken.refresh_token },
                        {"campmon_expireson",  DateTime.UtcNow.AddMinutes(-1).AddSeconds(newToken.expires_in) },
                    }
                };

                orgService.Update(updatedAuth);

                return new OAuthAuthenticationDetails(newToken.access_token, newToken.refresh_token);
            }
        }
    }
}
