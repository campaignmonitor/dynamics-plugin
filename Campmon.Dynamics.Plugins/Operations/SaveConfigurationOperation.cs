using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class SaveConfigurationOperation : IOperation
    {
        private ConfigurationService configService;
        private IOrganizationService orgService;
        private ITracingService trace;

        public SaveConfigurationOperation(IOrganizationService orgSvc, ConfigurationService configSvc, ITracingService tracer)
        {
            configService = configSvc;
            orgService = orgSvc;
            trace = tracer; 
        }

        public string Execute(string serializedData)
        {
            trace.Trace("Deserializing input.");
            var userInput = JsonConvert.DeserializeObject<ConfigurationData>(serializedData);
            trace.Trace("Loading current configuration.");
            var oldConfig = configService.VerifyAndLoadConfig();
            var accessToken = oldConfig != null ? oldConfig.AccessToken : configService.GetAccessToken();
            var auth = Authenticator.GetAuthentication(accessToken);

            var updatedConfig = new CampaignMonitorConfiguration
            {
                AccessToken = accessToken,
                ClientId = userInput.Clients.First().ClientID,
                ClientName = userInput.Clients.First().Name,
                ListId = userInput.Lists.First().ListID,
                ListName = userInput.Lists.First().Name,
                SyncDuplicateEmails = userInput.SyncDuplicateEmails,
                SubscriberEmail = (SubscriberEmailValues)userInput.SubscriberEmail,
                SyncFields = userInput.Fields.Select(f => f.LogicalName),
                SyncViewId = userInput.Views != null ? userInput.Views.First().ViewId : Guid.Empty,
                SyncViewName = userInput.Views != null ? userInput.Views.First().ViewName : null
            };

            if (string.IsNullOrEmpty(updatedConfig.ListId))
            {
                // create a new list
                trace.Trace("Creating new list {0}", updatedConfig.ListName);
                updatedConfig.ListId = List.Create(auth, updatedConfig.ClientId, updatedConfig.ListName, null, userInput.ConfirmedOptIn, null, UnsubscribeSetting.OnlyThisList);
            }

            configService.SaveConfig(updatedConfig);

            var metadata = new MetadataHelper(orgService, trace);

            if (oldConfig != null)
            {
                var newFields = updatedConfig.SyncFields.Except(oldConfig.SyncFields);
                var removedFields = oldConfig.SyncFields.Except(updatedConfig.SyncFields);
                // create new custom fields

                // delete removed custom fields
            }
            else
            {
                var cmService = new CampaignMonitorService(updatedConfig);
                // create all custom fields
                var attributes = metadata.GetEntityAttributes("contact");
                foreach(var syncField in userInput.Fields)
                {
                    var attribute = attributes
                        .Where(a => a.LogicalName == syncField.LogicalName)
                        .First();

                    var displayName = attribute.DisplayName.UserLocalizedLabel.Label;
                    var dataType = MapDynamicsTypeToCampmonType(attribute.AttributeType.Value);
                    var newKey = cmService.CreateCustomField(updatedConfig.ListId, displayName, dataType);
                }
            }

            // todo: kick off workflow
            return "saved";
        }

        private CustomFieldDataType MapDynamicsTypeToCampmonType(AttributeTypeCode dynamicsType)
        {
            switch (dynamicsType)
            {
                case AttributeTypeCode.DateTime:
                    return CustomFieldDataType.Date;
                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.Double:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.Money:
                    return CustomFieldDataType.Number;
                default:
                    return CustomFieldDataType.Text;
            }
        }
    }
}
