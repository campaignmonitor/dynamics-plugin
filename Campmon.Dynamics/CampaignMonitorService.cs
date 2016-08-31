using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics
{
    public class CampaignMonitorService
    {
        private CampaignMonitorConfiguration _config { get; set; }
        private AuthenticationDetails auth { get; set; }

        public CampaignMonitorService(CampaignMonitorConfiguration config, IOrganizationService orgService)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;
            auth = Authenticator.GetAuthentication(config, orgService);
        }

        public General GetAuthGeneral()
        {
            return new General(auth);
        }

        public string CreateCustomField(string listId, string name, CustomFieldDataType type)
        {
            var list = new List(auth, listId);
            var sanitizedName = SanitizeKeyName(name);
            try
            {
                return list.CreateCustomField(sanitizedName, type, null);
            }
            catch(Exception ex)
            {
                throw new Exception(String.Format("Error creating custom field '{0}'. {1}", name, ex.Message));
            }
        }

        public void DeleteCustomField(string listId, string name)
        {
            var list = new List(auth, listId);
            var fieldKey = name.StartsWith("[") && name.EndsWith("]")
                ? name
                : string.Format("[{0}]", name.Replace(" ", ""));

            try
            {
                list.DeleteCustomField(SanitizeKeyName(fieldKey));
            }
            catch(Exception ex)
            {
                throw new Exception(String.Format("Error deleting field {0}. {1}", name, ex.Message));
            }
        }

        private string SanitizeKeyName(string name)
        {
            return name.Replace("/", "").Replace("\\", "");
        }
 
    }
}
