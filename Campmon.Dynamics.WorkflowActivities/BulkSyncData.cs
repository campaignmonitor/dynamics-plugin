using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class BulkSyncData
    {
        public BulkSyncData()
        {

        }

        public string PagingCookie { get; set; }
        public int PageNumber { get; set; }
        public string[] UpdatedFields { get; set; }
    }
}
