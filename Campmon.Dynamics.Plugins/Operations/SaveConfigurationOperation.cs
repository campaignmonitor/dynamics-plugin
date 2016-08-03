using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class SaveConfigurationOperation : IOperation
    {
        private ConfigurationService configService;

        public SaveConfigurationOperation(ConfigurationService configSvc)
        {
            configService = configSvc;    
        }

        public string Execute(string serializedData)
        {

            var userInput = JsonConvert.DeserializeObject<ConfigurationData>(serializedData);
            var currentConfig = configService.VerifyAndLoadConfig();

            var updatedConfig = new CampaignMonitorConfiguration
            {

            };

            configService.SaveConfig(updatedConfig);

            // todo: kick off workflow
            return "";
        }
    }
}
