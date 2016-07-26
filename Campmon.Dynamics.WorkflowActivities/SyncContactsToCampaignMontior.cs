using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Campmon.Dynamics.WorkflowActivities.Logic;

namespace Campmon.Dynamics.WorkflowActivities
{
    class SyncContactsToCampaignMontior : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.InitiatingUserId);
            var tracer = executionContext.GetExtension<ITracingService>();

            var logic = new SyncContactsToCampaignMonitorLogic(orgService, tracer);
            var output = logic.Execute();

            Output.Set(executionContext, output);
            throw new NotImplementedException();
        }

        [Output("Completed Process")]
        [Default("")]
        public OutArgument<bool> Output { get; set; }
    }
}
