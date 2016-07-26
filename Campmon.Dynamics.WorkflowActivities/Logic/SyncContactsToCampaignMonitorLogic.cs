using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk;

namespace Campmon.Dynamics.WorkflowActivities.Logic
{
    class SyncContactsToCampaignMonitorLogic
    {
        IOrganizationService _orgService;
        ITracingService _tracer;

        public SyncContactsToCampaignMonitorLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public bool Execute()
        {
            throw new NotImplementedException();
        }
    }
}
