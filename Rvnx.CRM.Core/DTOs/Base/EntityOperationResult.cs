namespace Rvnx.CRM.Core.DTOs.Base;

/// <summary>
/// Shared result shape for entity CRUD operations exposed via DTOs.
/// Holds the outcome flags and error messages; derived types add a typed entity id
/// and their own <c>Ok</c>/<c>NotFound</c> factories.
/// </summary>
public abstract class EntityOperationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsNotFound { get; set; }
}
