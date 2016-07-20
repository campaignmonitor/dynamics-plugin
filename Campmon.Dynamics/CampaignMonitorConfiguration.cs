using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics
{
    public class CampaignMonitorConfiguration
    {
        public string AccessToken { get; set; }
        public string BulkSyncData { get; set; }
        public bool BulkSyncInProgress { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ListId { get; set; }
        public string ListName { get; set; }
        public string SetUpError { get; set; }
        public bool SyncDuplicateEmails { get; set; }
        public IEnumerable<string> SyncFields { get; set; }
        public Guid SyncViewId { get; set; }
        public string SyncViewName { get; set; }
    }
}
