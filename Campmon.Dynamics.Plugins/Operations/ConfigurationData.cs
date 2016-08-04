using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using createsend_dotnet;

namespace Campmon.Dynamics.Plugins.Operations
{
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
        public bool ConfirmedOptIn { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ListId { get; set; }
        public string ListName { get; set; }

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
