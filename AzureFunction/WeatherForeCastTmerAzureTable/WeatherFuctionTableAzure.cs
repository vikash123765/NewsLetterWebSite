using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WeatherForeCastTmerAzureTable
{
    public class WeatherFunctionTableAzure
    {
        private readonly HttpClient _httpClient;
        private readonly TableClient _tableClient;
        private readonly ILogger<WeatherFunctionTableAzure> _logger;

        public WeatherFunctionTableAzure(IConfiguration configuration, ILogger<WeatherFunctionTableAzure> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;

            string storageConnectionString = configuration["AzureStorage"];
            _tableClient = new TableClient(storageConnectionString, "WeatherForecasts");
            _tableClient.CreateIfNotExists();
        }

        // TimerTrigger: Runs every 1 minutes
        [Function("FetchWeatherData_Timer")]
        public async Task Run([TimerTrigger("*/1 * * * *")] TimerInfo myTimer)
        {
            await FetchWeatherDataInternal();
        }

        // HTTP Trigger for manual testing (optional)
        [Function("FetchWeatherData")]
        public async Task<IActionResult> FetchWeatherData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
        {
            await FetchWeatherDataInternal();
            return new OkObjectResult("Weather data fetched and stored.");
        }

        // Common logic for fetching and storing weather data
        private async Task FetchWeatherDataInternal()
        {
            string city = "Stockholm";
            _logger.LogInformation($"⏳ Fetching weather data for {city} at {DateTime.UtcNow}");

            try
            {
                string url = $"http://weatherapi.dreammaker-it.se/forecast?city={city}&lang=en";
                string responseJson = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrEmpty(responseJson))
                {
                    _logger.LogWarning("⚠️ No data received from API.");
                    return;
                }

                var jsonData = JsonSerializer.Deserialize<JsonElement>(responseJson);
                // Get the current local time (this is the machine's local time)
                DateTime localTime = DateTime.Now;

                // Convert the local time to UTC
                DateTime utcTime = localTime.ToUniversalTime();

                // Convert the UTC time to Swedish time using the Swedish time zone ID
                TimeZoneInfo swedishZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                DateTime swedishTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, swedishZone);

                // Explicitly mark the Swedish time as UTC for storage purposes (Azure requires DateTime with Kind = Utc)
                DateTime swedishTimeUtc = DateTime.SpecifyKind(swedishTime, DateTimeKind.Utc);



                var entity = new WeatherEntity
                {
                    PartitionKey = "WeatherData",
                    // Using Guid for RowKey ensures each record is unique.
                    RowKey = Guid.NewGuid().ToString(),
                    City = jsonData.GetProperty("city").GetString(),
                    TemperatureC = jsonData.GetProperty("temperatureC").GetInt32(),
                    TemperatureF = 32 + (int)(jsonData.GetProperty("temperatureC").GetInt32() / 0.5556),
                    Humidity = jsonData.GetProperty("humidity").GetInt32(),
                    WindSpeed = jsonData.GetProperty("windSpeed").GetInt32(),
                    Summary = jsonData.GetProperty("summary").GetString(),
                    Date = swedishTimeUtc
                };

                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation($"✅ Weather data stored for {entity.City}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching weather data: {ex.Message}");
            }
        }
    }
}