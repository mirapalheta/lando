using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using HealthChecks.ApplicationStatus.DependencyInjection;
using Lando;
using Lando.FunctionApp.Converters;
using Lando.FunctionApp.Security;
using Lando.Security;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Initialize the Lando ActivitySource exactly once at process start so its lifetime is
// explicit rather than tied to any registration callback.
_ = new ActivitySource("Lando");

// FunctionsApplication.CreateBuilder + ConfigureFunctionsWebApplication runs the full
// .NET Generic Host lifecycle, which means IHostedService implementations (e.g.
// ChangeReportService) are actually started. HttpRequestData-style triggers keep working
// alongside ASP.NET Core-style triggers.
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration.AddEnvironmentVariables();

builder.Logging.AddDebug();

// The Functions Worker registers FunctionsWorkerLoggerProvider, which forwards ILogger
// writes to the Functions Host over gRPC. That provider drops messages produced outside
// a function invocation context, which silently swallows everything our hosted services,
// singletons, and DI factories try to log. Adding a Console provider in parallel writes
// the same log entries straight to the container's stdout, where Container Apps' log
// stream captures them reliably. Console is additive; per-invocation logs still flow
// through the Functions pipeline as well.
builder.Logging.AddSimpleConsole(o =>
{
    o.IncludeScopes = false;
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss.fff ";
});

var services = builder.Services;
var configuration = builder.Configuration;

services.AddHomeAssistant();
services.AddAlexaSmartHome();

services.AddAzureClients(b =>
{
    var uri = configuration["KEY_VAULT_URI"]
        ?? throw new InvalidOperationException("KEY_VAULT_URI env var required");
    b.AddSecretClient(new Uri(uri));
    b.UseCredential(new DefaultAzureCredential());
});
services.AddSingleton<ISecretClient, KeyVaultSecretClient>();
services.AddSingleton<ITokenStoreFactory, TokenStoreFactory>();

// Health-check tags: emit assembly metadata (branch, commit, version) so /api/health
// makes it easy to confirm exactly which build is running.
var tags = AppDomain.CurrentDomain.GetAssemblies()
    .Where(w => w.FullName?.StartsWith("Lando", StringComparison.InvariantCultureIgnoreCase) is true)
    .SelectMany(s => s.GetCustomAttributes<AssemblyMetadataAttribute>())
    .Where(w => w.Key != "RepositoryUrl")
    .Select(s => $"{s.Key}:{s.Value}")
    .Concat([$"startupTime:{DateTime.UtcNow:o}"])
    .Distinct();

services.AddHealthChecks().AddApplicationStatus(tags: tags).AddHomeAssistant();

services.Configure<JsonSerializerOptions>(o =>
{
    o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.Converters.Add(new JsonDateTimeConverter());
    o.Converters.Add(new JsonDoubleConverter());
    o.Converters.Add(new JsonExceptionConverter());
    o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});


await builder.Build().RunAsync().ConfigureAwait(false);
