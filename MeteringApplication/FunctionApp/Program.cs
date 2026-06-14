using Azure.Data.Tables;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using SmartMetering.AzureFunctions.Domain;
using SmartMetering.AzureFunctions.Infrastructure.Persistence;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();
//.UseAzureMonitorExporter();

builder.Services.AddSingleton(_ =>
{
    var connectionString = builder.Configuration["AzureWebJobsStorage"];
    return new TableServiceClient(connectionString);
});

builder.Services.AddScoped<ITelemetrijaRepository, TelemetrijaRepository>();

builder.Services.AddDbContext<SmartMeteringDbContext>(options =>
{
    var sqlConnectionString = builder.Configuration["SqlConnectionString"];
    options.UseSqlServer(sqlConnectionString);
});

builder.Services.AddScoped<IBrojiloRepository, BrojiloRepository>();

builder.Build().Run();