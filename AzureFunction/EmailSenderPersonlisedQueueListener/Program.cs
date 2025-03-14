using EmailSenderPersonlisedQueueListener.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<EmailSender>(); // Add email sender service
    })
    .Build();

host.Run();
