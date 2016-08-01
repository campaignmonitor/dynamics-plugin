using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics.WorkflowActivities.Model
{
    public class BulkSyncError
    {
        public BulkSyncError(string code, string message, string email)
        {
            ErrorCode = code;
            ErrorMessage = message;
            Email = email;            
        }

        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Email { get; set; }        
    }
}
