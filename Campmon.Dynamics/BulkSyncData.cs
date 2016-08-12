using System.Collections.Generic;

namespace Campmon.Dynamics
{
    public class BulkSyncData
    {
        public BulkSyncData()
        {

        }

        public string PagingCookie { get; set; }
        public int PageNumber { get; set; }
        public string[] UpdatedFields { get; set; }

        public List<BulkSyncError> BulkSyncErrors { get; set; }
        public int NumberInvalidEmails { get; set; }
    }
}
