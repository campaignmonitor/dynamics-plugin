using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace Campmon.Dynamics
{
    public static class SuccessEmailSender
    {
        private const string serviceUrl = "https://integrationstore-5d74b11ccbdb8fa8.microservice.createsend.com/campaign-monitor-for-dynamics/email/data-synced";

        public static bool SendEmail(string accessToken, string requestBody)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                var emailSend = httpClient.SendAsync(request);
                emailSend.Wait();

                if (emailSend.Result.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
