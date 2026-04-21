namespace Rvnx.CRM.Core.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected during an entity update operation.
/// This abstraction prevents infrastructure-specific concurrency exceptions (e.g., DbUpdateConcurrencyException)
/// from leaking into the Core or Web layers.
/// </summary>
public class EntityConcurrencyException : Exception
{
    public EntityConcurrencyException()
        : base("A concurrency error occurred. The entity may have been modified or deleted since it was loaded.")
    {
    }

    public EntityConcurrencyException(string message)
        : base(message)
    {
    }

    public EntityConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
