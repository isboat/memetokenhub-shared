namespace MemeTokenHub.Shared.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "MongoDb";
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
}
