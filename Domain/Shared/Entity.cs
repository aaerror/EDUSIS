namespace Domain.Shared;

public abstract class Entity : IEquatable<Entity>
{
	private List<IDomainEvent> _eventos;

	public Guid Id { get; protected set; }
	public IReadOnlyCollection<IDomainEvent> Eventos { get => _eventos?.ToList().AsReadOnly(); }


	#region CONSTRUCTOR
	protected Entity() {}

	protected Entity(Guid id)
		: base() => Id = id;
	#endregion

	#region Events
	protected void AgregarEvento(IDomainEvent nuevoEvento)
	{
		_eventos = _eventos ?? new List<IDomainEvent>();
		_eventos.Add(nuevoEvento);
	}

	protected void QuitarEvento(IDomainEvent unEvento) =>
		_eventos?.Remove(unEvento);

	public void LiberarEventos() =>
		_eventos?.Clear();
	#endregion

	public bool Equals(Entity? other) =>
		Equals((object?) other);

	public override bool Equals(object? obj) =>
		obj is Entity entity && Id.Equals(entity.Id);

	public static bool operator ==(Entity left, Entity right) =>
		Equals(left, right);

	public static bool operator !=(Entity left, Entity right) =>
		!Equals(left, right);

	public override int GetHashCode() =>
		Id.GetHashCode();
}