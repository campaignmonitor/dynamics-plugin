using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonomaPartners.Crm.Toolkit;
using SonomaPartners.Crm.Toolkit.Plugins;
using Campmon.Dynamics.Plugins.Operations;
using Newtonsoft.Json;
namespace Campmon.Dynamics.Plugins
{
    public class ExecuteOperationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            /*
              campmon_ExecuteOperationAction
              Input:
                OperationName
                InputData
              Output:
                OutputData             
            */
            var operationFactory = new Dictionary<string, Func<IOperation>>
            {
                { "getclientlist", ()=> new GetClientList() },
                { "loadmetadata", () => new LoadMetadataOperation() }
            };

            var pluginContext = serviceProvider.GetPluginExecutionContext();

            var operationName = pluginContext.InputParameters["OperationName"] as string;
            var inputData = pluginContext.InputParameters["InputData"] as string;

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
