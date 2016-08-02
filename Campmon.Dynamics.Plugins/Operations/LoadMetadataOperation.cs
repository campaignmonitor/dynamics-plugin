using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk.Query;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class LoadMetadataOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;
        private IOrganizationService orgService;

        public LoadMetadataOperation(ConfigurationService configSvc, IOrganizationService orgSvc, ITracingService tracer)
        {
            configService = configSvc;
            trace = tracer;
            orgService = orgSvc;
        }

        public string Execute(string serializedData)
        {
            ConfigurationData config = new ConfigurationData();
            try
            {
                config = BuildConfigurationData(serializedData);
            }
            catch (Exception ex)
            {
                config.Error = $"Unable to retrieve configuration data. {ex.Message}";
            }

            return JsonConvert.SerializeObject(config);
        }

        private ConfigurationData BuildConfigurationData(string serializedData)
        {
            var output = new ConfigurationData();

            var config = configService.VerifyAndLoadConfig();
            if (config == null)
            {
                return output;
            }

            output.ConfigurationExists = true;

            var auth = Authenticator.GetAuthentication(config);
            var general = new General(auth);

            var clients = new General(auth).Clients();
            output.Clients = clients;

            if (clients.Count() == 1)
            {
                var client = new Client(auth, clients.First().ClientID);
                output.Lists = client.Lists();
            }

            output.BulkSyncInProgress = config.BulkSyncInProgress;
            output.SyncDuplicateEmails = config.SyncDuplicateEmails;
            output.SubscriberEmail = config.SubscriberEmail != null ? config.SubscriberEmail.Value : default(int);

            var views = GetContactViews();
            foreach(var view in views.Where(v => v.ViewId == config.SyncViewId))
            {
                view.IsSelected = true;
            }

            output.Views = views;

            return output;
        }

        private IEnumerable<SyncView> GetContactViews()
        {
            var query = new QueryExpression("savedquery"); // system views
            query.ColumnSet = new ColumnSet("savedqueryid", "name");
            query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, 2); // contacts
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // active state
            query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0); // application views


            var result = orgService.RetrieveMultiple(query);

            return result.Entities.Select(e => new SyncView
            {
                ViewId = e.Id,
                ViewName = e.GetAttributeValue<string>("name")
            });
        }
    }

    public class ConfigurationData
    {
        public string Error { get; set; }
        public bool BulkSyncInProgress { get; set; }
        public bool ConfigurationExists { get; set; }
        public IEnumerable<BasicClient> Clients { get; set; }
        public IEnumerable<BasicList> Lists { get; set; }
        public IEnumerable<SyncField> Fields { get; set; }
        public IEnumerable<SyncView> Views { get; set; }
        public bool SyncDuplicateEmails { get; set; }
        public int SubscriberEmail { get; set; }
        
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SyncField
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool IsChecked { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class SyncView
    {
        public Guid ViewId { get; set; }
        public string ViewName { get; set; }
        public bool IsSelected { get; set; }
    }
}
