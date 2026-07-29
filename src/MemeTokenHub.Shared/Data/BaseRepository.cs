using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MemeTokenHub.Shared.Data;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T?> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly IMongoCollection<T> Collection;

    public BaseRepository(IMongoDatabase database, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        Collection = database.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await Collection.Find(IdFilter(id)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Collection.Find(FilterDefinition<T>.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task<T?> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var result = await Collection.ReplaceOneAsync(IdFilter(id), entity, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.MatchedCount == 0 ? null : entity;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await Collection.DeleteOneAsync(IdFilter(id), cancellationToken).ConfigureAwait(false);
        return result.DeletedCount > 0;
    }

    internal static FilterDefinition<T> IdFilter(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var idMember = BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap
            ?? throw new InvalidOperationException($"{typeof(T).Name} does not define a BSON ID member.");
        var serializer = idMember.GetSerializer();
        object idValue = serializer.ValueType == typeof(ObjectId)
            ? ObjectId.Parse(id)
            : serializer.ValueType == typeof(Guid)
                ? Guid.Parse(id)
                : id;
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            writer.WriteStartDocument();
            writer.WriteName("_id");
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), idValue);
            writer.WriteEndDocument();
        }
        return new BsonDocumentFilterDefinition<T>(document);
    }
}
