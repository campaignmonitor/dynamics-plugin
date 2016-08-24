using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Campmon.Dynamics.Utilities
{
    /// <summary>
    /// Extension methods for IOrganizationService.
    /// </summary>
    public static class OrgServiceExtensions
    {
        #region Retrieve extensions

        /// <summary>
        /// Retrieve a strongly-typed entity from the organization service.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="er">EntityReference of target record.</param>
        /// <param name="attributeNames">Attribute names to be retrieved.</param>
        /// <returns>Strongly-typed entity.</returns>
        public static T Retrieve<T>(this IOrganizationService service, EntityReference er, params string[] attributeNames)
            where T : Entity, new()
        {
            return service.Retrieve<T>(er, new ColumnSet(attributeNames));
        }

        /// <summary>
        /// Retrieve a strongly-typed entity from the organization service.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="er">EntityReference of target record.</param>
        /// <param name="columns">ColumnSet to be retrieved.</param>
        /// <returns>Strongly-typed entity.</returns>
        public static T Retrieve<T>(this IOrganizationService service, EntityReference er, ColumnSet columns)
            where T : Entity, new()
        {
            var entity = service.Retrieve(er.LogicalName, er.Id, columns);

            if (entity != null)
            {
                return entity.ToEntity<T>();
            }
            else
            {
                return new T();
            }
        }

        /// <summary>
        /// Retrieve a strongly-typed entity from the organization service.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="logicalName">Logical name of target record.</param>
        /// <param name="id">Id of target record.</param>
        /// <param name="attributeNames">Attributes to be retrieved.</param>
        /// <returns>Strongly-typed entity.</returns>
        public static T Retrieve<T>(this IOrganizationService service, string logicalName, Guid id, params string[] attributeNames)
            where T : Entity, new()
        {
            return service.Retrieve<T>(logicalName, id, new ColumnSet(attributeNames));
        }

        /// <summary>
        /// Retrieve a strongly-typed entity from the organization service.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="logicalName">Logical name of target record.</param>
        /// <param name="id">Id of target record.</param>
        /// <param name="columns">Columns to be retrieved.</param>
        /// <returns>Strongly-typed entity.</returns>
        public static T Retrieve<T>(this IOrganizationService service, string logicalName, Guid id, ColumnSet columns)
            where T : Entity, new()
        {
            var entity = service.Retrieve(logicalName, id, columns);

            if (entity != null)
            {
                return entity.ToEntity<T>();
            }
            else
            {
                return new T();
            }
        }

        /// <summary>
        /// Retrieve an entity from the organization service by EntityReference.
        /// </summary>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="er">EntityReference of target record.</param>
        /// <param name="columns">Columns to be retrieved.</param>
        /// <returns>Entity record.</returns>
        public static Entity Retrieve(this IOrganizationService service, EntityReference er, ColumnSet columns)
        {
            if (er == null)
            {
                return null as Entity;
            }

            return service.Retrieve(er.LogicalName, er.Id, columns);
        }

        #endregion

        #region RetrieveMultiple extensions
        /// <summary>
        /// Retrieve multiple strongly-typed entities from the organization service.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">IOrganizationService instance.</param>
        /// <param name="query">Query to execute.</param>
        /// <returns>Strongly-typed IEnumerable of the query results.</returns>
        public static IEnumerable<T> RetrieveMultiple<T>(this IOrganizationService service, QueryBase query)
            where T : Entity, new()
        {
            var result = service.RetrieveMultiple(query);

            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                return result.Entities.Select(e => e.ToEntity<T>());
            }
            else
            {
                return Enumerable.Empty<T>();
            }
        }

        public static EntityCollection RetrieveMultipleAll(this IOrganizationService orgService, string fetchXml)
        {
            return orgService.RetrieveMultipleAll(fetchXml, 1, null, 5000);
        }

        /// <summary>
        /// Retrieve multiple pages of results for a fetch query in a single EntityCollection.
        /// </summary>
        /// <param name="orgService">IOrganizationService instance.</param>
        /// <param name="fetchXml">Fetch string to execute.</param>
        /// <param name="pageNumber">Page number to start from. Default is 1.</param>
        /// <param name="pagingCookie">Paging cookie to use. Default is null.</param>
        /// <param name="count">Number of records to retrieve. Default is 5000.</param>
        /// <returns>EntityCollection of all results from the query.</returns>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService orgService, string fetchXml,
            int pageNumber, string pagingCookie, int count)
        {
            fetchXml = AddPagingAttributes(fetchXml, pagingCookie, pageNumber, count);

            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            EntityCollection entityCollection = new EntityCollection();
            bool moreRecords = false;

            do
            {
                request.Query = new FetchExpression(fetchXml);

                var results = ((RetrieveMultipleResponse)orgService.Execute(request)).EntityCollection;
                entityCollection.Entities.AddRange(results.Entities);
                moreRecords = results.MoreRecords;
                pageNumber++;
                fetchXml = AddPagingAttributes(fetchXml, results.PagingCookie, pageNumber, count);
            }
            while (moreRecords);

            return entityCollection;
        }

        private static string AddPagingAttributes(string fetchXml, string cookie, int page, int count)
        {
            var doc = XDocument.Parse(fetchXml);

            var fetchXmlAttribute = doc.Element("fetch");

            fetchXmlAttribute.SetAttributeValue("page", page);
            fetchXmlAttribute.SetAttributeValue("count", count);
            fetchXmlAttribute.SetAttributeValue("paging-cookie", cookie);

            return fetchXmlAttribute.Document.Root.ToString();
        }

        /// <summary>
        /// Retrieve multiple pages of results for a QueryExpression in a single EntityCollection.
        /// </summary>
        /// <param name="orgService">IOrganization service instance.</param>
        /// <param name="query">QueryExpression to execute.</param>
        /// <returns>EntityCollection of all results from the query.</returns>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService orgService, QueryExpression query)
        {
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;

            EntityCollection entityCollection = new EntityCollection();
            bool moreRecords = false;
            query.PageInfo.Count = 5000;
            query.PageInfo.PageNumber = 1;

            do
            {
                var results = orgService.RetrieveMultiple(query);
                entityCollection.Entities.AddRange(results.Entities);
                moreRecords = results.MoreRecords;
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
            }
            while (moreRecords);

            return entityCollection;
        }
        #endregion

        #region ExecuteMultiple extensions

        /// <summary>
        /// Executes an ExecuteMultipleRequest for a collection of requests, returning responses and continuing on error.
        /// </summary>
        /// <param name="service">IOrganizationService.</param>
        /// <param name="requestCollection">Collection of requests to execute.</param>
        /// <returns>ExecuteMultipleResponse with CRM responses.</returns>
        public static ExecuteMultipleResponse ExecuteMultiple(this IOrganizationService service, IEnumerable<OrganizationRequest> requestCollection)
        {
            return service.ExecuteMultiple(requestCollection, true, true);
        }

        /// <summary>
        /// Executes an ExecuteMultipleRequest for a collection of requests.
        /// </summary>
        /// <param name="service">IOrganizationService.</param>
        /// <param name="requestCollection">Collection of requests to execute.</param>
        /// <param name="continueOnError">If set to <c>true</c>, continue executing requests if one encounters an error.</param>
        /// <param name="returnResponses">If set to <c>true</c>, return responses for successful requests.</param>
        /// <returns>ExecuteMultipleResponse.</returns>
        /// <exception cref="System.ArgumentException">requestCollection must be 1000 requests or less</exception>
        public static ExecuteMultipleResponse ExecuteMultiple(this IOrganizationService service, IEnumerable<OrganizationRequest> requestCollection,
            bool continueOnError, bool returnResponses)
        {
            if (requestCollection.Count() > 1000) throw new ArgumentException("requestCollection must be 1000 requests or less");

            var settings = new ExecuteMultipleSettings
            {
                ContinueOnError = continueOnError,
                ReturnResponses = returnResponses
            };

            return service.ExecuteMultiple(requestCollection, settings);
        }

        /// <summary>
        /// Executes an ExecuteMultipleRequest for a collection of requests with specified settings.
        /// </summary>
        /// <param name="service">IOrganizationService.</param>
        /// <param name="requestCollection">Collection of requests to execute.</param>
        /// <param name="requestSettings">The request settings.</param>
        /// <returns>ExecuteMultipleResponse.</returns>
        public static ExecuteMultipleResponse ExecuteMultiple(this IOrganizationService service, IEnumerable<OrganizationRequest> requestCollection,
            ExecuteMultipleSettings requestSettings)
        {
            if (requestCollection.Count() > 1000) throw new ArgumentException("requestCollection must be 1000 requests or less");

            var executeCollection = new OrganizationRequestCollection();
            executeCollection.AddRange(requestCollection);

            var request = new ExecuteMultipleRequest
            {
                Settings = requestSettings,
                Requests = executeCollection
            };

            return (ExecuteMultipleResponse)service.Execute(request);
        }

        /// <summary>
        /// Executes an ExecuteMultipleRequest for every set of 1000 requests in the collection, returning responses and continuing on error.
        /// </summary>
        /// <param name="service">IOrganizationService.</param>
        /// <param name="requestCollection">Collection of requests to execute.</param>
        /// <returns>Collection of ExecuteMultipleResponse for the executed ExecuteMultipleRequests.</returns>
        public static IEnumerable<ExecuteMultipleResponse> ExecuteMultipleAll(this IOrganizationService service, IEnumerable<OrganizationRequest> requestCollection)
        {
            var responseList = new List<ExecuteMultipleResponse>();

            foreach (var batch in requestCollection.Batch(1000))
            {
                var response = service.ExecuteMultiple(batch, true, true);

                responseList.Add(response);
            }

            return responseList;
        }

        #endregion
    }
}
