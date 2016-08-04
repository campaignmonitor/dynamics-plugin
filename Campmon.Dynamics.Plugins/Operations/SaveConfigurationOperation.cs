using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class SaveConfigurationOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;

        public SaveConfigurationOperation(ConfigurationService configSvc, ITracingService tracer)
        {
            configService = configSvc;
            trace = tracer; 
        }

        public string Execute(string serializedData)
        {
            trace.Trace("Deserializing input.");
            var userInput = JsonConvert.DeserializeObject<ConfigurationData>(serializedData);
            trace.Trace("Loading current configuration.");
            var currentConfig = configService.VerifyAndLoadConfig();

            var updatedConfig = new CampaignMonitorConfiguration
            {
                ClientId = userInput.Clients.First().ClientID,
                ClientName = userInput.Clients.First().Name,
                ListId = userInput.Lists.First().ListID,
                ListName = userInput.Lists.First().Name,
                SyncDuplicateEmails = userInput.SyncDuplicateEmails,
                SubscriberEmail = (SubscriberEmailValues)userInput.SubscriberEmail,
                SyncFields = userInput.Fields.Select(f => f.LogicalName),
                SyncViewId = userInput.Views != null ? userInput.Views.First().ViewId : Guid.Empty,
                SyncViewName = userInput.Views != null ? userInput.Views.First().ViewName : null
            };
            
            configService.SaveConfig(updatedConfig);

            // todo: create custom fields and kick off workflow
            return "saved";
        }
    }
}
