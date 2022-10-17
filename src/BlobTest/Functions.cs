using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlobTest
{
    public class Functions
    {
        [FunctionName("HttpEndpoint")]
        public static async Task<HttpResponseMessage> HttpEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "person")]
            HttpRequest req,
            ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();

            Common.Person myClass = JsonConvert.DeserializeObject<Common.Person>(content);

            return new HttpResponseMessage() { StatusCode = HttpStatusCode.OK };
        }

        [FunctionName("BlobQueue")]
        public static void BlobQueue([QueueTrigger("reporting", Connection = "")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }

        [FunctionName("RemoteQueue")]
        public static void RemoteQueue([QueueTrigger("reporting", Connection = "")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
