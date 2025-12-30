namespace SMS.Domain.Common;

/// <summary>
/// Base class for aggregate roots with domain event support
/// </summary>
public abstract class AggregateRoot : TenantEntity, IAggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
