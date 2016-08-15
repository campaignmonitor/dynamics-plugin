using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class SaveConfigurationOperation : IOperation
    {
        private readonly Guid BULK_SYNC_WORKFLOW_ID = new Guid("C5C1ADE9-81C8-4EFD-9D32-98F1EBBF3B92");
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
            var auth = Authenticator.GetAuthentication(oldConfig);

            var updatedConfig = new CampaignMonitorConfiguration
            {
                AccessToken = oldConfig.AccessToken,
                RefreshToken = oldConfig.RefreshToken,
                BulkSyncInProgress = userInput.BulkSyncInProgress,
                ClientId = userInput.Clients.First().ClientID,
                ClientName = userInput.Clients.First().Name,
                Id = string.IsNullOrWhiteSpace(userInput.Id) ? Guid.Empty : new Guid(userInput.Id),
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

            if (oldConfig != null 
                && oldConfig.ClientId == updatedConfig.ClientId 
                && oldConfig.ListId == updatedConfig.ListId)
            {
                var cmService = new CampaignMonitorService(updatedConfig);
                var attributes = metadata.GetEntityAttributes("contact");
                var newFields = updatedConfig.SyncFields.Except(oldConfig.SyncFields);
                var removedFields = oldConfig.SyncFields.Except(updatedConfig.SyncFields);

                // create new custom fields
                foreach (var fieldName in newFields)
                {
                    var attribute = attributes
                        .Where(a => a.LogicalName == fieldName)
                        .First();

                    var displayName = attribute.DisplayName.UserLocalizedLabel.Label;
                    trace.Trace("Creating new field {0}", displayName);
                    var dataType = MapDynamicsTypeToCampmonType(attribute.AttributeType.Value);
                    var newKey = cmService.CreateCustomField(updatedConfig.ListId, displayName, dataType);

                }
                // delete removed custom fields
                foreach(var fieldName in removedFields)
                {
                    var attribute = attributes
                        .Where(a => a.LogicalName == fieldName)
                        .First();

                    var displayName = attribute.DisplayName.UserLocalizedLabel.Label;
                    trace.Trace("Deleting field {0}", displayName);
                    cmService.DeleteCustomField(updatedConfig.ListId, displayName);
                }
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
                    trace.Trace("Creating new field {0}", displayName);
                    var dataType = MapDynamicsTypeToCampmonType(attribute.AttributeType.Value);
                    var newKey = cmService.CreateCustomField(updatedConfig.ListId, displayName, dataType);
                }
            }

            
            // if updatedConfig doesn't have an Id, it was a new config.
            // it was already saved above, so reload to get the Id
            if (updatedConfig.Id == Guid.Empty)
            {
                updatedConfig = configService.VerifyAndLoadConfig();
            }

            ExecuteWorkflowRequest workFlowReq = new ExecuteWorkflowRequest
            {
                WorkflowId = BULK_SYNC_WORKFLOW_ID,
                EntityId = updatedConfig.Id
            };

            ExecuteWorkflowResponse workflowResp = (ExecuteWorkflowResponse)orgService.Execute(workFlowReq);            
            
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
