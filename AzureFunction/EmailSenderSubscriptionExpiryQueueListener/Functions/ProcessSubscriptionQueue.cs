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
using EmailSenderSubscriptionExpiryQueueListener.Services;
using EmailSenderSubscriptionExpiryQueueListener.Models;

namespace EmailSenderSubscriptionExpiryQueueListener.Functions
{
    public class ProcessSubscriptionQueue
    {
        private readonly ILogger _logger;
        private readonly EmailSender _emailSender;

        public ProcessSubscriptionQueue(ILoggerFactory loggerFactory, EmailSender emailSender)
        {
            _logger = loggerFactory.CreateLogger<ProcessSubscriptionQueue>();
            _emailSender = emailSender;
        }

        // ✅ QUEUE TRIGGER FUNCTION
        [Function("ProcessQueueMessage")]
        public async Task Run(
            [QueueTrigger("testqueue", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            string decodedMessage = DecodeMessage(queueMessage);
            await ProcessMessage(decodedMessage);
        }

        // ✅ HTTP TRIGGER TO MANUALLY PROCESS QUEUE
        [Function("StartQueueListener")]
        public async Task RunHttp([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("📢 Manually triggering queue listener...");

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var queueClient = new QueueClient(connectionString, "manual-testqueue"); // 🔄 Separate queue for manual testing

            if (!await queueClient.ExistsAsync())
            {
                _logger.LogWarning("⚠️ Queue 'manual-testqueue' does not exist.");
                return;
            }

            QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages: 5);

            if (messages.Length == 0)
            {
                _logger.LogInformation("✅ No new messages in queue.");
                return;
            }

            foreach (var message in messages)
            {
                string decodedMessage = DecodeMessage(message.MessageText);
                await ProcessMessage(decodedMessage);
                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
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
                List<UserSubscription>? subscriptions = JsonSerializer.Deserialize<List<UserSubscription>>(
                    queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (subscriptions == null || subscriptions.Count == 0)
                {
                    _logger.LogError("❌ Deserialization returned null or empty list.");
                    return;
                }

                _logger.LogInformation($"✅ Successfully deserialized {subscriptions.Count} users.");

                // ✅ Process each subscription
                foreach (var subscription in subscriptions)
                {
                    _logger.LogInformation($"🔹 Processing {subscription.UserName} ({subscription.Email})");

                    if (subscription.ExpiringSubscriptions == null || subscription.ExpiringSubscriptions.Count == 0)
                    {
                        _logger.LogWarning($"⚠️ No expiring subscriptions for {subscription.UserName}");
                        continue;
                    }

                    foreach (var expiringSub in subscription.ExpiringSubscriptions)
                    {
                        _logger.LogInformation($"⏳ Expiring Subscription: {expiringSub.SubscriptionType} - {expiringSub.ExpiryDate}");
                    }

                    await _emailSender.SendEmailAsync(subscription);
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


        // ✅ HELPER FUNCTION: CHECK IF STRING IS BASE64 ENCODED
        private bool IsBase64String(string input)
        {
            Span<byte> buffer = new Span<byte>(new byte[input.Length]);
            return Convert.TryFromBase64String(input, buffer, out _);
        }
    }
}
