namespace SMS.Domain.Common;

/// <summary>
/// Interface for aggregate roots that can raise domain events
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
