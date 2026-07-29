using MongoDB.Bson;
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

    private static FilterDefinition<T> IdFilter(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return ObjectId.TryParse(id, out var objectId)
            ? Builders<T>.Filter.Eq("_id", objectId)
            : Builders<T>.Filter.Eq("_id", id);
    }
}
