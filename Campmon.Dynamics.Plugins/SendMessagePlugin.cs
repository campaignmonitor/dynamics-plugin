using System;
using Campmon.Dynamics.Utilities;
using Campmon.Dynamics.Plugins.Logic;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Plugins
{
    public class SendMessagePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("SendMessagePlugin");

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
