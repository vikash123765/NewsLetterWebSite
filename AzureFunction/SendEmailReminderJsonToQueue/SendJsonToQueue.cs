using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SendEmailReminderJsonToQueue
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Azure.Storage.Queues;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;

    public class SendJsonToQueue
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public SendJsonToQueue(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<SendJsonToQueue>();
        }

        [Function("SendJsonToQueue")]
        public async Task<HttpResponseData> Run(
             [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            // Avoid processing duplicate requests from preflight OPTIONS calls.
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var optionsResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await optionsResponse.WriteStringAsync("OPTIONS request - no processing needed.");
                return optionsResponse;
            }

            _logger.LogInformation($"HTTP trigger function executed at: {DateTime.Now}");

            // External API URL (replace with your actual endpoint if needed)
            string apiUrl = "http://localhost:5101/GetExpiringSubscriptionsJson";
            string jsonResponse = string.Empty;

            try
            {
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully retrieved JSON from API.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling API: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error calling API: " + ex.Message);
                return errorResponse;
            }

            // Retrieve storage connection string from environment variable.
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string queueName = "testqueue";

            try
            {
                QueueClient queueClient = new QueueClient(connectionString, queueName);
                await queueClient.CreateIfNotExistsAsync();
                string base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonResponse));
                await queueClient.SendMessageAsync(base64Message);

                _logger.LogInformation("Message added to queue successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message to queue: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error sending message to queue: " + ex.Message);
                return errorResponse;
            }

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await okResponse.WriteStringAsync("Message added to queue successfully.");
            return okResponse;
        }
    }
}