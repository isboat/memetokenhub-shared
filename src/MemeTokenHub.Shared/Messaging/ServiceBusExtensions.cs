using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemeTokenHub.Shared.Messaging;

public static class ServiceBusExtensions
{
    /// <summary>Registers Service Bus and a service-owned durable processed-event store.</summary>
    public static IServiceCollection AddMemeTokenHubServiceBus<TProcessedEventStore>(this IServiceCollection services, IConfiguration configuration)
        where TProcessedEventStore : class, IProcessedEventStore
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ServiceBus:ConnectionString is required.");
        services.AddSingleton(new ServiceBusClient(connectionString));
        services.AddSingleton<IProcessedEventStore, TProcessedEventStore>();
        return services;
    }

    /// <summary>Registers non-durable event deduplication for tests and local development only.</summary>
    public static IServiceCollection AddMemeTokenHubInMemoryEventDeduplication(this IServiceCollection services)
    {
        services.AddSingleton<IProcessedEventStore, InMemoryProcessedEventStore>();
        return services;
    }
}
