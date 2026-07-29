using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MemeTokenHub.Shared.Configuration;

public static class MongoExtensions
{
    public static IServiceCollection AddMemeTokenHubMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>().Bind(configuration.GetSection(MongoOptions.SectionName)).Validate(
            x => !string.IsNullOrWhiteSpace(x.ConnectionString) && !string.IsNullOrWhiteSpace(x.DatabaseName),
            "MongoDb connection string and service-owned database name are required.").ValidateOnStart();
        services.AddSingleton<IMongoClient>(provider => new MongoClient(provider.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });
        return services;
    }
}
