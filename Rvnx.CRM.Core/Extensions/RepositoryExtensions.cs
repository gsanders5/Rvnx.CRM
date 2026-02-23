using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Core.Extensions;

public static class RepositoryExtensions
{
    /// <summary>
    /// Explicitly chunks a large list of keys to avoid SQL parameter limits and executes queries against the repository layer in batches.
    /// This resolves correctness issues with invisible implicit batching where complex predicates (like negations) might return semantically incorrect unioned results across chunks.
    /// </summary>
    public static async Task<List<T>> ListByChunkedContainsAsync<T, TKey>(
        this IRepository repository,
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, Expression<Func<T, bool>>> predicateBuilder,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default,
        params string[] includes) where T : BaseEntity
    {
        List<T> results = [];
        
        foreach (TKey[] chunk in keys.Chunk(1000))
        {
            if (chunk.Length == 0) continue;
            
            Expression<Func<T, bool>> predicate = predicateBuilder(chunk);
            
            List<T>? chunkResults = asNoTracking 
                ? await repository.ListAsNoTrackingAsync(predicate, cancellationToken, includes)
                : await repository.ListAsync(predicate, cancellationToken, includes);
                
            if (chunkResults != null)
            {
                results.AddRange(chunkResults);
            }
        }
        
        return results;
    }
}
