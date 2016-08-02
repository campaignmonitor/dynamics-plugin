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
            var configService = new ConfigurationService(serviceProvider.CreateSystemOrganizationService(), serviceProvider.GetTracingService());
            
            var trace = serviceProvider.GetTracingService();

            var operationFactory = new Dictionary<string, Func<IOperation>>
            {
                { "getclients", () => new GetClients(configService) },
                { "getclientlist", ()=> new GetClientList(configService) },
                { "loadmetadata", () => new LoadMetadataOperation(configService, trace) }
            };

            var pluginContext = serviceProvider.GetPluginExecutionContext();

            var operationName = pluginContext.InputParameters["OperationName"] as string;
            var inputData = pluginContext.InputParameters["InputData"] as string;

            trace.Trace($"Operation: {operationName} Input: {inputData}");

            if (!operationFactory.ContainsKey(operationName))
                return;

            var operation = operationFactory[operationName].Invoke();
            string outputData = null;

            try
            {
                outputData = operation.Execute(inputData);
            }
            catch (Exception ex)
            {
                //todo: serialize exception for output
                outputData = JsonConvert.SerializeObject($"An error occured. Message: {ex.Message}");
            }

            pluginContext.OutputParameters["OutputData"] = outputData;
        }
    }
}
