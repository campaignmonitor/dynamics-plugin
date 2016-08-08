using System;
using Microsoft.Xrm.Sdk;
using Campmon.Dynamics.Plugins.Logic;
using SonomaPartners.Crm.Toolkit.Plugins;

namespace Campmon.Dynamics.Plugins
{
    public class ContactSyncPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();
            var isUpdate = context.MessageName == "Update";

            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }
            
            Entity target = (Entity)context.InputParameters["Target"];
            Entity postEntityImage = context.GetPostEntityImage("contact");
            
            ContactSyncLogic syncLogic = new ContactSyncLogic(orgService, tracer);
            syncLogic.SyncContact(target, postEntityImage, isUpdate);
        }
    }
}