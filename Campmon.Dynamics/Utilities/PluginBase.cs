using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Utilities
{
    /// <summary>
    /// Abstract base class for Plugins. Implements <see cref="T:Microsoft.Xrm.Sdk.IPluginExecutionContext"></see> as required by the CRM SDK and can execute custom code by overriding one of the OnExecute, OnCreate, OnUpdate, or OnDelete functions.
    /// </summary>
    /// <seealso cref="Microsoft.Xrm.Sdk.IPlugin" />
    public abstract class PluginBase : IPlugin
    {
        /// <summary>
        /// Executes plug-in code in response to an event.
        /// </summary>
        /// <param name="serviceProvider">Type: Returns_IServiceProvider. A container for service objects. Contains references to the plug-in execution context (<see cref="T:Microsoft.Xrm.Sdk.IPluginExecutionContext"></see>), tracing service (<see cref="T:Microsoft.Xrm.Sdk.ITracingService"></see>), organization service (<see cref="T:Microsoft.Xrm.Sdk.IOrganizationServiceFactory"></see>), and notification service (<see cref="T:Microsoft.Xrm.Sdk.IServiceEndpointNotificationService"></see>).</param>
        /// <exception cref="System.ArgumentNullException">serviceProvider</exception>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) { throw new ArgumentNullException("serviceProvider"); }

            OnExecute(serviceProvider);
        }

        /// <summary>
        /// Called when the plugin is executed for any message.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public virtual void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            if (context.MessageName.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                OnCreate(serviceProvider);
            }
            else if (context.MessageName.Equals("update", StringComparison.OrdinalIgnoreCase))
            {
                OnUpdate(serviceProvider);
            }
            else if (context.MessageName.Equals("delete", StringComparison.OrdinalIgnoreCase))
            {
                OnDelete(serviceProvider);
            }
        }

        /// <summary>
        /// Called when the plugin is executed for a create message if OnExecute is not overriden.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public virtual void OnCreate(IServiceProvider serviceProvider)
        {
        }

        /// <summary>
        /// Called when the plugin is executed for an update message if OnExecute is not overriden.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public virtual void OnUpdate(IServiceProvider serviceProvider)
        {
        }

        /// <summary>
        /// Called when the plugin is executed for a delete message if OnExecute is not overriden.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public virtual void OnDelete(IServiceProvider serviceProvider)
        {
        }

    }
}
