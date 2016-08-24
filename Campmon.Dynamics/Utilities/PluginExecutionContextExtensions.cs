using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Utilities
{
    /// <summary>
    /// Extension methods for IPluginExecutionContext
    /// </summary>
    public static class IPluginExecutionContextExtensions
    {
        /// <summary>
        /// Gets the target entity from plugin context input parameters and casts to type T.
        /// </summary>
        /// <typeparam name="T">Strongly-typed Entity.</typeparam>
        /// <param name="context">The context.</param>
        /// <returns>Target entity cast to type T.</returns>
        /// <exception cref="System.ArgumentNullException">context</exception>
        public static T GetTargetEntity<T>(this IPluginExecutionContext context) where T : Entity
        {
            if (context == null) { throw new ArgumentNullException("context"); }

            return context.GetTargetEntity().ToEntity<T>();
        }

        /// <summary>
        /// Gets the target entity from plugin context input parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Target entity.</returns>
        /// <exception cref="System.ArgumentNullException">context</exception>
        public static Entity GetTargetEntity(this IPluginExecutionContext context)
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            return context.GetInputParameter<Entity>("Target");
        }

        /// <summary>
        /// Gets the named input parameters and attempts to cast as type T.
        /// </summary>
        /// <typeparam name="T">Type of the input parameter.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>Input parameter as type T, or null if does not cast.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// parameterName
        /// </exception>
        public static T GetInputParameter<T>(this IPluginExecutionContext context, string parameterName) where T : class
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (parameterName == null) { throw new ArgumentNullException("parameterName"); }

            var target = context.GetInputParameter(parameterName);

            // TODO: should probably throw an exception if the cast fails.
            return target as T;
        }

        /// <summary>
        /// Gets the named input parameter.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>The input parameter as <c>object</c> type.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// parameterName
        /// </exception>
        /// <exception cref="Microsoft.Xrm.Sdk.InvalidPluginExecutionException">
        /// Unable to retrieve parameter from context.
        /// </exception>
        public static object GetInputParameter(this IPluginExecutionContext context, string parameterName)
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (parameterName == null) { throw new ArgumentNullException("parameterName"); }

            object target;
            if (!context.InputParameters.TryGetValue(parameterName, out target))
            {
                throw new InvalidPluginExecutionException(string.Format("Unable to retrieve {0} from context.", parameterName));
            }

            return target;
        }

        /// <summary>
        /// Gets the entity pre image as type T.
        /// </summary>
        /// <typeparam name="T">Strongly-typed Entity.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="preImageName">Name of the pre image.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// preImageName
        /// </exception>
        public static T GetPreEntityImage<T>(this IPluginExecutionContext context, string preImageName) where T : Entity
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (preImageName == null) { throw new ArgumentNullException("preImageName"); }

            return context.GetPreEntityImage(preImageName).ToEntity<T>();
        }

        /// <summary>
        /// Gets the entity pre image.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="preImageName">Name of the pre image.</param>
        /// <returns>Pre Image Entity instance.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// preImageName
        /// </exception>
        public static Entity GetPreEntityImage(this IPluginExecutionContext context, string preImageName)
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (preImageName == null) { throw new ArgumentNullException("preImageName"); }

            return GetEntityImage(context.PreEntityImages, preImageName);
        }

        /// <summary>
        /// Gets the entity post image.
        /// </summary>
        /// <typeparam name="T">Strongly-typed Entity.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="postImageName">Name of the post image.</param>
        /// <returns>Entity Post Image cast to type T.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// postImageName
        /// </exception>
        public static T GetPostEntityImage<T>(this IPluginExecutionContext context, string postImageName) where T : Entity
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (postImageName == null) { throw new ArgumentNullException("postImageName"); }

            return context.GetPostEntityImage(postImageName).ToEntity<T>();
        }

        /// <summary>
        /// Gets the post entity image.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="postImageName">Name of the post image.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// context
        /// or
        /// postImageName
        /// </exception>
        public static Entity GetPostEntityImage(this IPluginExecutionContext context, string postImageName)
        {
            if (context == null) { throw new ArgumentNullException("context"); }
            if (postImageName == null) { throw new ArgumentNullException("postImageName"); }

            return GetEntityImage(context.PostEntityImages, postImageName);
        }

        /// <summary>
        /// Gets the entity image from an image collection.
        /// </summary>
        /// <param name="imageCollection">The image collection.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <returns>Entity</returns>
        /// <exception cref="System.ArgumentNullException">
        /// imageCollection
        /// or
        /// imageName
        /// </exception>
        /// <exception cref="Microsoft.Xrm.Sdk.InvalidPluginExecutionException">Unable to retrieve image from context.</exception>
        private static Entity GetEntityImage(EntityImageCollection imageCollection, string imageName)
        {
            if (imageCollection == null) { throw new ArgumentNullException("imageCollection"); }
            if (imageName == null) { throw new ArgumentNullException("imageName"); }

            Entity image;
            if (!imageCollection.TryGetValue(imageName, out image) || image == null)
            {
                throw new InvalidPluginExecutionException("Unable to retrieve image from context.");
            }

            return image;
        }
    }
}
