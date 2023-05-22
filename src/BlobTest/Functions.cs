using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlobTest
{
    public class AppendResponse
    {
        public Boolean Existing { get; set; } = false;
    }

    public class Functions
    {
        private string _ConnectionString = "UseDevelopmentStorage=true";
        private string _BlobContainer = "logs";

        /// <summary>
        /// Http Endpoint that receives the initial data
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("HttpEndpoint")]
        public async Task<HttpResponseMessage> HttpEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "log")]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                // Get the content of the request body
                var content = await new StreamReader(req.Body).ReadToEndAsync();

                // Unpack the body and try and convert it to a known type
                Common.Log data = JsonConvert.DeserializeObject<Common.Log>(content);

                // Generate the content of the queue message we want to pass on to the next stage (e.g. writing to blob)
                string messageContent = JsonConvert.SerializeObject(data);

                // Send the message on
                InsertMessage("logs", messageContent);

                // Report back to the caller that the data is "safe" but not yet processed
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.Accepted }; // Not "OK" as we have stored the data for further processing
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) }; // A bit too generic for now, but analyse error type before deciding
            }
        }

        /// <summary>
        /// Add a message to a given local queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        public void InsertMessage(string queueName, string message)
        {
            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(_ConnectionString, queueName, new QueueClientOptions
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

        /// <summary>
        /// Trigger to anything that enters the blob queue so we can take that message and process it to blob before sending on anywhere else
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("BlobQueue")]
        public void BlobQueue([QueueTrigger("logs", Connection = "AzureWebJobsStorage")] string message, ILogger log)
        {
            log.LogInformation($"Processing For Blob: {message}");

            // Unpack the body and try and convert it to a known type
            Common.Log toWrite = JsonConvert.DeserializeObject<Common.Log>(message);

            // Append the data to the blob
            AppendBlob(CreateBlobName(toWrite), message);

            log.LogInformation($"Message written to next queue");
        }

        private string CreateBlobName(Common.Log log)
        {
            string path = log.Account.ToString(); //person.Account.Chunk(4).Select(charArray => new string(charArray)).Aggregate((partialPhrase, word) => $"{partialPhrase}/{word}");
            DateTime logTime = log.Timestamp;
            string dateComponent = $"{logTime.Year.ToString().PadLeft(2, '0')}/{logTime.Month.ToString().PadLeft(2, '0')}";
            string combinedPath = $"{path}/{dateComponent}.json";
            return combinedPath; // Only seperate so we can debug for now
        }

        /// <summary>
        /// Append some data to the end of an append block blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="data"></param>
        public void AppendBlob(string blobName, string data)
        {
            // Create a new connection / client to enable writing blobs
            BlobServiceClient blobServiceClient = new BlobServiceClient(_ConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_BlobContainer);
            containerClient.CreateIfNotExists();

            // Set up a client specifically to append data quickly
            var appendBlobClient = containerClient.GetAppendBlobClient(blobName);
            StringBuilder stringBuilder = new StringBuilder(data);
            if (appendBlobClient.Exists())  
                stringBuilder.Insert(0, $"{Environment.NewLine},");
            appendBlobClient.CreateIfNotExists();

            // As we are going to be well under the append limit we don't need to worry about splitting the writes
            // so just dump the byte stream to the appendblock method
            byte[] bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            Stream stream = new MemoryStream(bytes);
            stream.Position = 0;
            appendBlobClient.AppendBlock(stream);
        }
    }
}
