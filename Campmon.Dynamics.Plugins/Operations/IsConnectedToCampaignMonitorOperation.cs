using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class IsConnectedToCampaignMonitorOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;

        public IsConnectedToCampaignMonitorOperation(ConfigurationService configSvc, ITracingService tracer)
        {
            configService = configSvc;
            trace = tracer;
        }

        public string Execute(string serializedData)
        {
            trace.Trace("Loading current configuration.");
            var config = configService.VerifyAndLoadConfig();
            if(config == null)
            {
                trace.Trace("Missing or invalid campaign monitor configuration.");
                return "Missing or invalid campaign monitor configuration.";
            }
            
            if(String.IsNullOrWhiteSpace(config.AccessToken))
            {
                trace.Trace("NotConnected");
                return "NotConnected";
            }
            trace.Trace("Connected");
            return "Connected";

        }
    }
}
