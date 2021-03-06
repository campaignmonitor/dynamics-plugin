﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campmon.Dynamics.Utilities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class BulkSyncContacts : CodeActivity
    {
        protected override void Execute(CodeActivityContext activityContext)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var trace = activityContext.GetExtension<ITracingService>();
            trace.Trace("Starting BulkSyncContacts activity.");

            var workflowContext = activityContext.GetExtension<IWorkflowContext>();
            var serviceFactory = activityContext.GetExtension<IOrganizationServiceFactory>();
            var orgService = serviceFactory.CreateOrganizationService(null);

            var sync = new SyncHandler(orgService, trace, stopwatch);
            try
            {
                var result = sync.Run();

                SyncCompleted.Set(activityContext, result);
            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }

            trace.Trace("BulkSyncContacts activity finished.");
        }

        [Output("Is Sync Completed")]
        public OutArgument<bool> SyncCompleted { get; set; }
    }
}
