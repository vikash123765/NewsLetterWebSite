using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArchiveArticlesFunctionApp
{
    public class ArchiveArticlesFunction
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ArchiveArticlesFunction> _logger;
        private readonly string _archiveApiUrl;

        public ArchiveArticlesFunction(IConfiguration configuration, ILogger<ArchiveArticlesFunction> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            // Ensure ArchiveApiUrl points to your Web API Archive endpoint.
            _archiveApiUrl = configuration["ArchiveApiUrl"];
        }

        // TimerTrigger set to run every minute (for testing).
        // When you’re ready for production, you might change this CRON expression.
        [Function("ArchiveArticles_Timer")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timer)
        {
            _logger.LogInformation($"ArchiveArticles_Timer triggered at: {DateTime.UtcNow}");

            try
            {
                // Call the Web API Archive endpoint with a POST request.
                HttpResponseMessage response = await _httpClient.PostAsync(_archiveApiUrl, null);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Archive API responded: {result}");
                }
                else
                {
                    _logger.LogError($"Archive API returned status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Archive API: {ex.Message}");
            }
        }
    }
}