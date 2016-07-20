using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonomaPartners.Crm.Toolkit;
using SonomaPartners.Crm.Toolkit.Plugins;
using Campmon.Dynamics.Plugins.Operations;

namespace Campmon.Dynamics.Plugins
{
    public class ExecuteOperationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var operationFactory = new Dictionary<string, Func<IOperation>>
            {
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
                outputData = "An error occured";
            }

            pluginContext.OutputParameters["OutputData"] = outputData;
        }
    }
}
