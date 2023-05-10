using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Linq;

namespace BlobTest
{
    public class PersonMap : ClassMap<Common.Person>
    {
        public PersonMap()
        {
            Map(m => m.Id).Index(0).Name("id");
            Map(m => m.Name).Index(1).Name("name");
            Map(m => m.Surname).Index(2).Name("surname");
            Map(m => m.Address.AddressLine1).Index(3).Name("addressline1");
            Map(m => m.Address.PostalCode).Index(4).Name("postalcode");
        }
    }

    public class Functions
    {
        private string _ConnectionString = "UseDevelopmentStorage=true";
        private string _BlobContainer = "reports";

        /// <summary>
        /// Http Endpoint that receives the initial data
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("HttpEndpoint")]
        public async Task<HttpResponseMessage> HttpEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "person")]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                // Get the content of the request body
                var content = await new StreamReader(req.Body).ReadToEndAsync();

                // Unpack the body and try and convert it to a known type
                Common.Person person = JsonConvert.DeserializeObject<Common.Person>(content);

                // Generate the content of the queue message we want to pass on to the next stage (e.g. writing to blob)
                string messageContent = JsonConvert.SerializeObject(person);

                // Send the message on
                InsertMessage("reporting", messageContent);

                // Report back to the caller that the data is "safe" but not yet processed
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.Accepted }; // Not "OK" as we have stored the data for further processing
            }
            catch(Exception ex)
            {
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) }; // A bit too generic for now, but analyse error type before deciding
            }
        }

        /// <summary>
        /// Trigger to anything that enters the blob queue so we can take that message and process it to blob before sending on anywhere else
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("BlobQueue")]
        public void BlobQueue([QueueTrigger("reporting", Connection = "AzureWebJobsStorage")]string message, ILogger log)
        {
            log.LogInformation($"Processing For Blob: {message}");

            // Unpack the body and try and convert it to a known type
            Common.Person person = JsonConvert.DeserializeObject<Common.Person>(message);

            // Cast the person object to a csv line capable of being written to the blob
            string csv = PersonToCSV(person);

            // Append the data to the blob
            AppendBlob(CreateBlobName(person), csv);

            // Write the message to the next queue
            InsertMessage("remotes", message);

            log.LogInformation($"Message written to next queue");
        }

        private string CreateBlobName(Common.Person person)
        {
            string path = person.Account.Chunk(4).Select(charArray => new string(charArray)).Aggregate((partialPhrase, word) => $"{partialPhrase}/{word}");
            DateTime now = DateTime.Now;
            string dateComponent = $"{now.Year.ToString().PadLeft(2, '0')}/{now.Month.ToString().PadLeft(2, '0')}/{now.Day.ToString().PadLeft(2, '0')}";
            string combinedPath = $"{path}/{dateComponent}/{person.Account}.csv";
            return combinedPath; // Only seperate so we can debug for now
        }

        /// <summary>
        /// React to any messages placed on the reporting queue (Where we want to transmit the data elsewhere)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("RemoteQueue")]
        public void RemoteQueue([QueueTrigger("remotes", Connection = "AzureWebJobsStorage")] string message, ILogger log)
        {
            log.LogInformation($"Processing For Remote: {message}");

            log.LogInformation($"Message completed");
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
        /// Convert a person object to the CSV format to save to the blob
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        public string PersonToCSV(Common.Person person)
        {
            // https://joshclose.github.io/CsvHelper/getting-started/
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<PersonMap>(); // Tell it how to write the CSV
                    csv.WriteRecord(person);
                    csv.NextRecord(); // Force a line break
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
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
            appendBlobClient.CreateIfNotExists();

            // As we are going to be well under the append limit we don't need to worry about splitting the writes
            // so just dump the byte stream to the appendblock method
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            Stream stream = new MemoryStream(bytes);
            stream.Position = 0;
            appendBlobClient.AppendBlock(stream);
        }
    }
}
