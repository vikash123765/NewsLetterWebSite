using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsLetterBanan;

var builder = FunctionsApplication.CreateBuilder(args);

// Register HttpClient for dependency injection.
builder.Services.AddHttpClient();

// Configure the Functions Web Application.
builder.ConfigureFunctionsWebApplication();

var app = builder.Build();
app.Run();
