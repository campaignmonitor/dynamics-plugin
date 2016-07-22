using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Campmon.Dynamics;
using Microsoft.Crm.Sdk.Messages;

namespace Campmon.Dynamics.Plugins.Logic
{
    public class ContactSyncLogic
    {
        private ITracingService _tracer;
        private IOrganizationService _orgService;

        public ContactSyncLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        internal void SyncContact(Entity target, Entity postImage, bool isUpdate)
        {
            // 3. Check that the email field in campmon_emailaddress has data in the contact post image. If it is empty, exit the plugin.
            if (target == null || postImage == null || postImage["campmon_emailaddress"] == null)
            {
                return;
            }

            // 1. Retrieve the Campaign Monitor Configuration record. If it does not exist or is missing access token or client id, exit the plugin.
            ConfigurationService configService = new ConfigurationService(_orgService);
            CampaignMonitorConfiguration campaignMonitorConfig = configService.LoadConfig();
            if (campaignMonitorConfig == null ||
                    campaignMonitorConfig.AccessToken == null || campaignMonitorConfig.ClientId == null)
            {
                _tracer.Trace("Missing or invalid campaign monitor configuration.");
                return;
            }

            // 2. Retrieve the view specified in the campmon_syncviewid field of the configuration record.
            var filterView = RetrieveSyncFilter(campaignMonitorConfig.SyncViewId);
            var filterFetchXml = filterView["fetchxml"].ToString();

            // 4.Modify the sync view fetch query to include a filter condition for the current contact id.Execute the modified query and check if the contact is returned.If it is, exit the plugin.
            if (!TestContactFitsFilter(filterFetchXml, (Guid)target["contactid"]))
            {
                _tracer.Trace("Contact does not fit the filter.");
                return;
            }            

            // 5. If this is an update operation, check that the plugin target has modified attributes that are included in the campmon_syncfields data. If there are not any sync fields in the target, exit the plugin.

            /*
                6. Create a campmon_message record with the following data:
                    • campmon_sdkmessage = Plugin message (create or update)
                    • campmon_data = JSON serialized sync data
            */
            var syncMessage = new Entity("campmon_message");
            var syncData = SerializeDataToSync(target, campaignMonitorConfig);


            syncMessage["campmon_sdkmessage"] = isUpdate ? "update" : "create";
            syncMessage["campmon_data"] = syncData;
            _orgService.Create(syncMessage);
        }

        internal Entity RetrieveSyncFilter(Guid viewID)
        {
            return null;
        }

        internal bool TestContactFitsFilter(string fetchXml, Guid contactID)
        {
            return false;        
        }

        internal string SerializeDataToSync(Entity target, CampaignMonitorConfiguration config)
        {
            return string.Empty;
        }
    }
}