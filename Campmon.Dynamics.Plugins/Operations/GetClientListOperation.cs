using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class GetClientListOperation : IOperation
    {
        ConfigurationService _cmService { get; set; }
        public GetClientListOperation(ConfigurationService cmService)
        {
            _cmService = cmService;
        }

        public string Execute(string clientId)
        {
            var config = _cmService.VerifyAndLoadConfig();
            var auth = Authenticator.GetAuthentication(config);
            var client = new Client(auth, clientId);
            return JsonConvert.SerializeObject(client.Lists());
        }
    }
}
