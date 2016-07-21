using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class GetClientList : IOperation
    {
        public string Execute(string serializedData)
        {
            var clients = new string[] { "Client 1", "Client2" };

            return JsonConvert.SerializeObject(clients);
        }
    }
}
