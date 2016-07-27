using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campmon.Dynamics;
using Newtonsoft.Json;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class SyncHandler
    {
        private IOrganizationService orgService;
        private ITracingService trace;
        private Stopwatch timer;
        private CampaignMonitorConfiguration config;

        public SyncHandler(IOrganizationService organizationService, ITracingService tracingService, Stopwatch executionTimer)
        {
            orgService = organizationService;
            trace = tracingService;
            timer = executionTimer;

            var configService = new ConfigurationService(orgService, trace);
            trace.Trace("Loading configuration.");
            config = configService.VerifyAndLoadConfig();
        }

        public bool Run()
        {
            trace.Trace("Deserializing bulk sync data.");
            var syncData = JsonConvert.DeserializeObject<BulkSyncData>(config.BulkSyncData);

            // get view and modify.
            // if syncData.UpdatedFields contains data, only sync those fields.

            do
            {
                // sync batch of 1000 contacts to CM list as subscribers
            }
            while (timer.ElapsedMilliseconds >= 90000);

            return true;
            throw new NotImplementedException();
        }
    }
}
