using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using EmailSenderPersonlisedQueueListener.Models;
using EmailSenderPersonlisedQueueListener.Services;

namespace EmailSenderPersonlisedQueueListener.Functions
{
    public class ProcessPersonelisedQueue
    {
        private readonly ILogger _logger;
        private readonly EmailSender _emailSender;
        private readonly QueueClient _queueClient;

        public ProcessPersonelisedQueue(ILoggerFactory loggerFactory, EmailSender emailSender)
        {
            _logger = loggerFactory.CreateLogger<ProcessPersonelisedQueue>();
            _emailSender = emailSender;

            // ✅ Initialize QueueClient for manual queue processing
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _queueClient = new QueueClient(connectionString, "testqueue");
        }

        // ✅ TIMER TRIGGER FUNCTION TO PROCESS QUEUE AUTOMATICALLY
        [Function("ProcessArticleQueueMessage")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer) // Runs every 1 minute
        {
            _logger.LogInformation("⏳ Checking for new messages in 'articlequeue'...");

            if (!await _queueClient.ExistsAsync())
            {
                _logger.LogWarning("⚠️ Queue 'articlequeue' does not exist.");
                return;
            }

            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 5);

            if (messages.Length == 0)
            {
                _logger.LogInformation("✅ No new messages in queue.");
                return;
            }

            foreach (var message in messages)
            {
                string decodedMessage = DecodeMessage(message.MessageText);
                await ProcessMessage(decodedMessage);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }
        }

        // ✅ HTTP TRIGGER TO MANUALLY PROCESS QUEUE
        [Function("StartArticleQueueListener")]
        public async Task RunHttp([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("📢 Manually triggering article queue listener...");

            if (!await _queueClient.ExistsAsync())
            {
                _logger.LogWarning("⚠️ Queue 'articlequeue' does not exist.");
                return;
            }

            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 5);

            if (messages.Length == 0)
            {
                _logger.LogInformation("✅ No new messages in queue.");
                return;
            }

            foreach (var message in messages)
            {
                string decodedMessage = DecodeMessage(message.MessageText);
                await ProcessMessage(decodedMessage);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }
        }

        // ✅ PROCESS MESSAGE FUNCTION
        private async Task ProcessMessage(string queueMessage)
        {
            try
            {
                _logger.LogInformation($"📜 RAW queue message: {queueMessage}");

                if (string.IsNullOrWhiteSpace(queueMessage))
                {
                    _logger.LogError("❌ Received empty queue message.");
                    return;
                }

                // ✅ Ensure message is a valid JSON array
                if (!queueMessage.Trim().StartsWith("[") || !queueMessage.Trim().EndsWith("]"))
                {
                    _logger.LogError("❌ Queue message is not a valid JSON array.");
                    return;
                }

                // ✅ Attempt deserialization
                List<User>? users = JsonSerializer.Deserialize<List<User>>(
                    queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (users == null || users.Count == 0)
                {
                    _logger.LogError("❌ Deserialization returned null or empty list.");
                    return;
                }

                _logger.LogInformation($"✅ Successfully deserialized {users.Count} users.");

                // ✅ Process each user
                foreach (var user in users)
                {
                    _logger.LogInformation($"🔹 Processing user: {user.UserName} ({user.Email})");

                    if (user.LatestArticles == null || user.LatestArticles.Count == 0)
                    {
                        _logger.LogWarning($"⚠️ No latest articles for {user.UserName}");
                    }
                    else
                    {
                        foreach (var article in user.LatestArticles)
                        {
                            _logger.LogInformation($"📰 Latest Article: {article.Headline} by {article.Author}");
                        }
                    }

                    await _emailSender.SendEmailAsync(user);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"❌ JSON Deserialization Error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Unexpected error: {ex.Message}");
            }
        }

        private string DecodeMessage(string encodedMessage)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedMessage);
                return Encoding.UTF8.GetString(data);
            }
            catch (FormatException)
            {
                return encodedMessage; // Return as-is if not Base64
            }
        }
    }
}
