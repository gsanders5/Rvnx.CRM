namespace Rvnx.CRM.Core.DTOs.Base;

/// <summary>
/// Result shape for bulk operations that act on a collection of entities.
/// Counts the rows actually changed, those silently skipped (e.g. self-contact,
/// missing/cross-tenant ids, already-applied), and any error messages.
/// </summary>
public sealed record BulkOperationResult(int Successful, int Skipped, IReadOnlyList<string> Errors)
{
    public static BulkOperationResult Ok(int successful, int skipped = 0) =>
        new(successful, skipped, []);

    public static BulkOperationResult Fail(string error) =>
        new(0, 0, [error]);
}
