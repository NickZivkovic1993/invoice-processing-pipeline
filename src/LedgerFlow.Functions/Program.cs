using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using LedgerFlow.Core.Matching;
using LedgerFlow.Infrastructure.Erp;
using LedgerFlow.Infrastructure.Extraction;
using LedgerFlow.Infrastructure.Messaging;
using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Pipeline;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        var credential = new DefaultAzureCredential();

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddDbContext<LedgerFlowDbContext>(options =>
            options.UseSqlServer(config["SqlConnectionString"]));

        services.AddSingleton(_ => new DocumentIntelligenceClient(
            new Uri(config["DocumentIntelligenceEndpoint"]!), credential));
        services.AddSingleton(_ => new ServiceBusClient(config["ServiceBusConnection"], credential));
        services.AddSingleton(_ => new BlobServiceClient(
            new Uri(config["BlobServiceUri"] ?? "https://localhost/"), credential));

        services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();
        services.AddSingleton<IPipelineQueue>(sp =>
            new ServiceBusPipelineQueue(sp.GetRequiredService<ServiceBusClient>(), config["IngestQueueName"] ?? "invoices-in"));

        services.AddHttpClient<IErpPostingClient, HttpErpPostingClient>(c =>
            c.BaseAddress = new Uri(config["ErpBaseUrl"]!));
        services.AddHttpClient<IReferenceDataProvider, ErpReferenceDataProvider>(c =>
            c.BaseAddress = new Uri(config["ErpBaseUrl"]!));

        services.AddSingleton(new ThreeWayMatcher());
        services.AddScoped<InvoiceProcessor>();
    })
    .Build();

host.Run();
