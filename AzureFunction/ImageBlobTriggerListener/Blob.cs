using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImageBlobTriggerListener
{
    public class Blob
    {
        private readonly ILogger<Blob> _logger;

        public Blob(ILogger<Blob> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Blob))]
        public async Task Run([BlobTrigger("articleimages/{name}", Connection = "AzureWebJobsStorage")]Stream stream, string name)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] imageData = memoryStream.ToArray();

            _logger.LogInformation($"C# Blob trigger function processed an image.\n Name: {name} \n Size: {imageData.Length} bytes");

        }
    }
}
