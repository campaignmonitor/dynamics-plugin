using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonomaPartners.Crm.Toolkit;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.WorkflowActivities
{
    public class BulkSyncContacts : CodeActivity
    {
        protected override void Execute(CodeActivityContext activityContext)
        {
            var workflowContext = activityContext.GetExtension<IWorkflowContext>();
            var serviceFactory = activityContext.GetExtension<IOrganizationServiceFactory>();
            var orgService = serviceFactory.CreateOrganizationService(null);
            var trace = activityContext.GetExtension<ITracingService>();

            trace.Trace("Starting BulkSyncContacts activity.");



            trace.Trace("BulkSyncContacts activity finished.");
        }

        [Output("CustomerSalesInformation")]
        public OutArgument<bool> CustomerSalesInformation { get; set; }
    }
}
