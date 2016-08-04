using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SonomaPartners.Crm.Toolkit;
using SonomaPartners.Crm.Toolkit.Plugins;
using Campmon.Dynamics.Plugins.Operations;
using Newtonsoft.Json;

namespace Campmon.Dynamics.Plugins
{
    //  campmon_ExecuteOperationAction
    //  Input:
    //    OperationName
    //    InputData
    //  Output:
    //    OutputData        
    public class ExecuteOperationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var orgService = serviceProvider.CreateSystemOrganizationService();
            var configService = new ConfigurationService(orgService, serviceProvider.GetTracingService());
            
            var trace = serviceProvider.GetTracingService();

            var operationFactory = new Dictionary<string, Func<IOperation>>
            {
                { "getclients", () => new GetClients(configService) },
                { "getclientlist", ()=> new GetClientList(configService) },
                { "loadmetadata", () => new LoadMetadataOperation(configService, orgService, trace) },
                { "saveconfiguration", () => new SaveConfigurationOperation(orgService, configService, trace) }
            };

            var pluginContext = serviceProvider.GetPluginExecutionContext();

            var operationName = pluginContext.InputParameters["OperationName"] as string;
            var inputData = pluginContext.InputParameters["InputData"] as string;

            trace.Trace("Operation: {0} Input: {1}", operationName, inputData);

            if (!operationFactory.ContainsKey(operationName))
            {
                trace.Trace("Operation not defined.");
                return;
            }

            var operation = operationFactory[operationName].Invoke();
            string outputData = null;

            try
            {
                trace.Trace("Executing operation.");
                outputData = operation.Execute(inputData);
            }
            catch (Exception ex)
            {
                trace.Trace($"Fatal error: {ex.Message}");
                throw;
            }

            pluginContext.OutputParameters["OutputData"] = outputData;
        }
    }
}
