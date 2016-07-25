using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk;

namespace Campmon.Dynamics.Plugins.Logic
{
    class SendMessageLogic
    {
        IOrganizationService _orgService;
        ITracingService _tracer;

        public SendMessageLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void SendMessage(Entity target)
        {
            ConfigurationService configService = new ConfigurationService(_orgService);
            CampaignMonitorConfiguration campaignMonitorConfig = configService.LoadConfig();

            if (!campaignMonitorConfig.SyncDuplicateEmails)
            {
                // check if email already exists for any contact already synced 
                // if yes : set campmon_error on message to duplicate email 
            }

            ConvertSchemaNamesToCampaignMonitor();

            try
            {
                SendSubscriberToList(campaignMonitorConfig.ListId);
            }
            catch (Exception ex)
            {
                // if error on sending subscriber, set campmon_error on msg to the error
                return;
            }

            // deactivate msg if successful create/update
        }

        internal void ConvertSchemaNamesToCampaignMonitor()
        {
            // convert each field to Campaign Monitor custom 
            // field names by using the display name for the field
        }

        internal void SendSubscriberToList(string listId)
        {
            // send subscriber to campaign monitor list using CM API            
        }
    }
}
