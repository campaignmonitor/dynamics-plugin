using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class LoadMetadataOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;

        public LoadMetadataOperation(ConfigurationService service, ITracingService tracer)
        {
            configService = service;
            trace = tracer;
        }

        public string Execute(string serializedData)
        {
            var output = new ConfigurationData();

            var config = configService.VerifyAndLoadConfig();
            if (config == null)
            {
                return output.Serialize();
            }

            var auth = Authenticator.GetAuthentication(config);
            var general = new General(auth);

            var clients = new General(auth).Clients();
            output.Clients = clients;

            if (clients.Count() == 1)
            {
                var client = new Client(auth, clients.First().ClientID);
                output.Lists = client.Lists();
            }

            return output.Serialize();
        }
    }

    public class ConfigurationData
    {
        public string Error { get; set; }
        public bool BulkSyncInProgress { get; set; }
        public bool ConfigurationExists { get; set; }
        public IEnumerable<BasicClient> Clients { get; set; }
        public IEnumerable<BasicList> Lists { get; set; }
        public IEnumerable<SyncField> Fields { get; set; }
        public IEnumerable<SyncView> Views { get; set; }
        public bool SyncDuplicateEmails { get; set; }
        public int SubscriberEmail { get; set; }
        
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SyncField
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool IsChecked { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class SyncView
    {
        public Guid ViewId { get; set; }
        public string ViewName { get; set; }
        public bool IsSelected { get; set; }
    }
}
