using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Queues; // Namespace for Queue storage types
using Azure.Storage.Queues.Models; // Namespace for PeekedMessage
using System.Configuration; // Namespace for ConfigurationManager
using System;

namespace BlobTest
{
    public class Functions
    {
        [FunctionName("HttpEndpoint")]
        public async Task<HttpResponseMessage> HttpEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "person")]
            HttpRequest req,
            ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();

            Common.Person person = JsonConvert.DeserializeObject<Common.Person>(content);

            string messageContent = JsonConvert.SerializeObject(person);

            InsertMessage("reporting", messageContent);

            return new HttpResponseMessage() { StatusCode = HttpStatusCode.OK };
        }

        [FunctionName("BlobQueue")]
        public void BlobQueue([QueueTrigger("reporting", Connection = "AzureWebJobsStorage")]string message, ILogger log)
        {
            log.LogInformation($"Processing For Blob: {message}");

            // Write the message to the next queue
            InsertMessage("remotes", message);

            log.LogInformation($"Message written to next queue");
        }

        [FunctionName("RemoteQueue")]
        public void RemoteQueue([QueueTrigger("remotes", Connection = "AzureWebJobsStorage")] string message, ILogger log)
        {
            log.LogInformation($"Processing For Remote: {message}");

            log.LogInformation($"Message completed");
        }

        public void CreateQueueClient(string queueName)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);
        }

        public bool CreateQueue(string queueName)
        {
            try
            {
                // Get the connection string from app settings
                string connectionString = "UseDevelopmentStorage=true"; // ConfigurationManager.AppSettings["StorageConnectionString"];

                // Instantiate a QueueClient which will be used to create and manipulate the queue
                QueueClient queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });

                // Create the queue
                queueClient.CreateIfNotExists();

                if (queueClient.Exists())
                {
                    Console.WriteLine($"Queue created: '{queueClient.Name}'");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Make sure the Azurite storage emulator running and try again.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}\n\n");
                Console.WriteLine($"Make sure the Azurite storage emulator running and try again.");
                return false;
            }
        }

        public void InsertMessage(string queueName, string message)
        {
            // Get the connection string from app settings
            string connectionString = "UseDevelopmentStorage=true"; // ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

            // Create the queue if it doesn't already exist
            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                // Send a message to the queue
                queueClient.SendMessage(message);
            }

            Console.WriteLine($"Inserted: {message}");
        }
    }
}
