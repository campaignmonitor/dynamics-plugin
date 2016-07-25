using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonomaPartners.Crm.Toolkit.Plugins;
using SonomaPartners.Crm.Toolkit;
using Campmon.Dynamics.Plugins.Logic;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Plugins
{
    class SendMessagePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }

            Entity target = (Entity)context.InputParameters["Target"];

            SendMessageLogic logic = new SendMessageLogic(orgService, tracer);
            logic.SendMessage(target);
        }
    }
}
