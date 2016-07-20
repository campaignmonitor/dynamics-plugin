using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics.Plugins.Operations
{
    /// <summary>
    /// An operation to execute from the ExecuteOperationPlugin.
    /// </summary>
    interface IOperation
    {
        /// <summary>
        /// Execute the operation.
        /// </summary>
        /// <param name="serializedData">Serialized input data.</param>
        /// <returns></returns>
        string Execute(string serializedData);
    }
}
