# Performance Optimization Analysis: Paginated Repository Access

## Issue: Unsafe `ListAsync` Implementation
The original implementation of `ListAsync<T>` and `ListAsNoTrackingAsync<T>` in the `Repository` class fetched all records from the database table for a given entity type:

```csharp
public async Task<List<T>> ListAsync<T>(CancellationToken cancellationToken = default) where T : CRMBaseEntity
{
    return await _context.Set<T>().ToListAsync(cancellationToken);
}
```

### Inefficiency Details:
1.  **Memory Consumption (O(N))**: Loading an entire table into memory can lead to `OutOfMemoryException` if the table contains millions of rows.
2.  **I/O Latency**: Fetching large amounts of data from the database over the network is slow and consumes significant bandwidth.
3.  **CPU Usage**: Serialization and deserialization of large object graphs increase CPU pressure on both the database and the application server.

## Proposed Optimization
1.  **Pagination**: Introduce `skip` and `take` parameters to limit the number of records returned in a single call.
2.  **`IQueryable` Access**: Provide `GetQuery<T>()` and `GetQueryAsNoTracking<T>()` methods to allow callers to define their own filters and projections before executing the query against the database.

## Environment Constraints
Direct benchmarking (e.g., using BenchmarkDotNet) is impractical in this environment because:
1.  **Restricted Internet Access**: Cannot restore NuGet packages required for building and running a benchmark project.
2.  **Limited Tooling**: Standard profiling tools may not be available or functional without a complete build.

The optimization is justified based on industry best practices for data access patterns and the clear algorithmic improvement from O(N) to O(page_size) for most common use cases.
