using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
namespace Campmon.Dynamics.Plugins.Operations
{
    public class GetClients : IOperation
    {
        ConfigurationService _cmService { get; set; }
        public GetClients(ConfigurationService cmService)
        {
            _cmService = cmService;
        }

        public string Execute(string serializedData)
        {
            var auth = new ApiKeyAuthenticationDetails(_cmService.GetAccessToken());
            var clients = new General(auth).Clients();
            return JsonConvert.SerializeObject(clients);
        }
    }
}
