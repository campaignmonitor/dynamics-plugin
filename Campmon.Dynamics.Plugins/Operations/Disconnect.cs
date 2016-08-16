using System;
using System.Linq;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class Disconnect : IOperation
    {
        ConfigurationService _cmService { get; set; }
        public Disconnect(ConfigurationService cmService)
        {
            _cmService = cmService;
        }

        public string Execute(string d)
        {
            var config = _cmService.VerifyAndLoadConfig();

            config.BulkSyncData = 
                config.ClientId = 
                config.ClientName = 
                config.ListId = 
                config.ListName =
                config.SetUpError = 
                config.SyncViewName = string.Empty;

            config.SyncViewId = Guid.Empty;
            config.SyncFields = Enumerable.Empty<String>();           

            config.SyncDuplicateEmails =
                config.BulkSyncInProgress = false;

            _cmService.SaveConfig(config);
            _cmService.ClearOAuthToken(config.Id);
            return "Success";                    
        }
    }
}