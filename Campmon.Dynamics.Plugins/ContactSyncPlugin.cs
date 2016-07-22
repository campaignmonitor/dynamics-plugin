using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Campmon.Dynamics.Plugins.Logic;
using SonomaPartners.Crm.Toolkit.Plugins;
using SonomaPartners.Crm.Toolkit;

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
            Entity postEntityImage = (Entity)context.GetPostEntityImage("emailaddresses");
            
            ContactSyncLogic syncLogic = new ContactSyncLogic(orgService, tracer);
            syncLogic.SyncContact(target, null, isUpdate);
        }
    }
}