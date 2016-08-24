using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Campmon.Dynamics.Utilities
{
    /// <summary>
    /// Extension methods for IServiceProvider
    /// </summary>
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Get a service of type T from the service provider.
        /// </summary>
        /// <typeparam name="T">Type of service.</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>Instance of service type T.</returns>
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }

        /// <summary>
        /// Get the plugin execution context from the service provider.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns>IPluginExecutionContext instance.</returns>
        public static IPluginExecutionContext GetPluginExecutionContext(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IPluginExecutionContext>();
        }

        /// <summary>
        /// Create an instance of IOrganizationService for a user.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="userId">Id of user to call the service.</param>
        /// <returns>Instance of IOrganizationService.</returns>
        public static IOrganizationService CreateOrganizationService(this IServiceProvider serviceProvider, Guid userId)
        {
            var factory = serviceProvider.GetService<IOrganizationServiceFactory>();
            return factory.CreateOrganizationService(userId);
        }

        /// <summary>
        /// Creates the system organization service.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider from the plugin.</param>
        /// <returns>Instance of IOrganizationService</returns>
        public static IOrganizationService CreateSystemOrganizationService(this IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IOrganizationServiceFactory>();
            return factory.CreateOrganizationService(null);
        }

        /// <summary>
        /// Create an instance of IOrganizationService for the current user executing the plugin.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider from the plugin.</param>
        /// <returns>Instance of IOrganizationService.</returns>
        public static IOrganizationService CreateOrganizationServiceAsCurrentUser(this IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IOrganizationServiceFactory>();
            var context = serviceProvider.GetPluginExecutionContext();
            return factory.CreateOrganizationService(context.UserId);
        }

        /// <summary>
        /// Get the tracing service from the service provider.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns>ITracingService instance.</returns>
        public static ITracingService GetTracingService(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<ITracingService>();
        }

        /// <summary>
        /// Create an OrganizationServiceContext.
        /// </summary>
        /// <typeparam name="T">Type of OrganizationServiceContext.</typeparam>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="userId">Id of the user to call the service.</param>
        /// <returns>Instance of OrganizationServiceContext type T.</returns>
        public static T CreateOrganizationContext<T>(this IServiceProvider serviceProvider, Guid userId) where T : OrganizationServiceContext
        {
            var orgService = serviceProvider.CreateOrganizationService(userId);
            var constructor = typeof(T).GetConstructor(new Type[] { typeof(IOrganizationService) });
            return (T)constructor.Invoke(new object[] { orgService });
        }

        /// <summary>
        /// Create an OrganizationServiceContext for the system user.
        /// </summary>
        /// <typeparam name="T">Type of OrganizationServiceContext.</typeparam>
        /// <param name="serviceProvider">Service provider.</param>
        /// <returns>Instance of OrganizationServiceContext type T.</returns>
        public static T CreateSystemOrganizationContext<T>(this IServiceProvider serviceProvider) where T : OrganizationServiceContext
        {
            var orgService = serviceProvider.CreateSystemOrganizationService();
            var constructor = typeof(T).GetConstructor(new Type[] { typeof(IOrganizationService) });
            return (T)constructor.Invoke(new object[] { orgService });
        }

        /// <summary>
        /// Create an OrganizationServiceContext for the current user executing the plugin.
        /// </summary>
        /// <typeparam name="T">Type of OrganizationServiceContext.</typeparam>
        /// <param name="serviceProvider">Service provider.</param>
        /// <returns>Instance of OrganizationServiceContext type T.</returns>
        public static T CreateOrganizationContextAsCurrentUser<T>(this IServiceProvider serviceProvider) where T : OrganizationServiceContext
        {
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var constructor = typeof(T).GetConstructor(new Type[] { typeof(IOrganizationService) });
            return (T)constructor.Invoke(new object[] { orgService });
        }
    }
}
