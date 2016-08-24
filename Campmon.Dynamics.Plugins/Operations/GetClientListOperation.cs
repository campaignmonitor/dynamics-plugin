using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class GetClientListOperation : IOperation
    {
        private ConfigurationService _cmService { get; set; }
        private IOrganizationService _orgService { get; set; }

        public GetClientListOperation(ConfigurationService cmService, IOrganizationService orgService)
        {
            _cmService = cmService;
            _orgService = orgService;
        }

        public string Execute(string clientId)
        {
            var config = _cmService.VerifyAndLoadConfig();
            var auth = Authenticator.GetAuthentication(config, _orgService);
            var client = new Client(auth, clientId);
            return JsonConvert.SerializeObject(client.Lists());
        }
    }
}
