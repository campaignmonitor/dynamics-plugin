using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class LoadMetadataOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;
        private IOrganizationService orgService;

        private static readonly string[] RecommendedFields = new[]
        {
            "address1_city",
            "address1_country",
            "address1_primarycontactname",
            "anniversary",
            "annualincome",
            "birthdate",
            "parentcustomerid",
            "department",
            "donotemail",
            "emailaddress1",
            "emailaddress2",
            "emailaddress3",
            "firstname",
            "fullname",
            "gendercode",
            "jobtitle",
            "lastusedincampaign",
            "lastname",
            "familystatuscode",
            "numberofchildren",
            "preferredcontactmethodcode",
            "statecode",
        };

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
                trace.Trace("Error in build configuration.");
                config.Error = $"Unable to retrieve configuration data. {ex.Message}";
            }

            return JsonConvert.SerializeObject(config);
        }

        private ConfigurationData BuildConfigurationData(string serializedData)
        {
            trace.Trace("Building configuration.");
            var output = new ConfigurationData();

            var config = configService.VerifyAndLoadConfig();

            if (config == null)
            {
                trace.Trace("No configuration available.");
                return output;
            }
            trace.Trace("Configuration loaded.");
            output.ConfigurationExists = true;

            var auth = Authenticator.GetAuthentication(config);
            var general = new General(auth);

            var clients = new General(auth).Clients();
            output.Clients = clients;

            if (clients.Count() == 1)
            {
                trace.Trace("Not agency account, retrieving lists.");
                var client = new Client(auth, clients.First().ClientID);
                output.Lists = client.Lists();
            }

            output.BulkSyncInProgress = config.BulkSyncInProgress;
            output.SyncDuplicateEmails = config.SyncDuplicateEmails;
            output.SubscriberEmail = config.SubscriberEmail != null ? config.SubscriberEmail.Value : default(int);

            output.Views = GetContactViews(config);
            output.Fields = GetContactFields(config);

            return output;
        }

        private IEnumerable<SyncField> GetContactFields(CampaignMonitorConfiguration config)
        {
            trace.Trace("Getting contact fields.");
            var metadataHelper = new MetadataHelper(orgService, trace);
            var attributes = metadataHelper.GetEntityAttributes("contact");

            return attributes
                .Where(a => a.DisplayName != null)
                .Where(a => a.IsValidForAdvancedFind.Value == true)
                .Select(a => new SyncField
                {
                    DisplayName = a.DisplayName?.UserLocalizedLabel?.Label,
                    LogicalName = a.LogicalName,
                    IsChecked = config.SyncFields.Contains(a.LogicalName),
                    IsRecommended = RecommendedFields.Contains(a.LogicalName)
                })
                .OrderBy(f => f.DisplayName);
        }

        private IEnumerable<SyncView> GetContactViews(CampaignMonitorConfiguration config)
        {
            trace.Trace("Getting contact views.");
            var query = new QueryExpression("savedquery"); // system views
            query.ColumnSet = new ColumnSet("savedqueryid", "name");
            query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, 2); // contacts
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // active state
            query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0); // application views

            var result = orgService.RetrieveMultiple(query);

            return result.Entities.Select(e => new SyncView
            {
                ViewId = e.Id,
                ViewName = e.GetAttributeValue<string>("name"),
                IsSelected = (e.Id == config.SyncViewId)
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
