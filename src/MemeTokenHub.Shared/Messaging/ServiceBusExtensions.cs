using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemeTokenHub.Shared.Messaging;

public static class ServiceBusExtensions
{
    public static IServiceCollection AddMemeTokenHubServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ServiceBus:ConnectionString is required.");
        services.AddSingleton(new ServiceBusClient(connectionString));
        services.AddSingleton<IProcessedEventStore, InMemoryProcessedEventStore>();
        return services;
    }
}
