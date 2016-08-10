using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
