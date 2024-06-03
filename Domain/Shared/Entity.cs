namespace Domain.Shared;

public abstract class Entity : IEquatable<Entity>
{
    private List<IDomainEvent> _eventos;

    public Guid Id { get; protected set; }
    public IReadOnlyCollection<IDomainEvent> Eventos { get => _eventos?.ToList().AsReadOnly(); }

    protected Entity() { }

    protected Entity(Guid id) : base() => Id = id;

    #region Events
    protected void AgregarEvento(IDomainEvent nuevoEvento)
    {
        _eventos = _eventos ?? new List<IDomainEvent>();
        _eventos.Add(nuevoEvento);
    }

    protected void QuitarEvento(IDomainEvent unEvento) => _eventos?.Remove(unEvento);

    public void LiberarEventos() => _eventos?.Clear();
    #endregion

    public bool Equals(Entity? other)
    {
        return Equals((object?) other);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Id.Equals(entity.Id);
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
