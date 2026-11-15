namespace Domain.Shared;

public abstract class Entity : IEquatable<Entity>
{
	private readonly List<IDomainEvent> _eventos = new();

	public Guid Id { get; protected set; }
	/// <summary>
	/// Vista de sólo lectura sobre la cola: nunca null y sin copiar la lista en cada acceso.
	/// Es viva, así que quien la recorra mientras se encolan eventos debe materializarla antes
	/// (el despacho de <c>GuardarCambiosAsync</c> lo hace con <c>ToList()</c>).
	/// </summary>
	public IReadOnlyCollection<IDomainEvent> Eventos => _eventos.AsReadOnly();


	#region CONSTRUCTOR
	protected Entity() {}

	protected Entity(Guid id)
		: base() => Id = id;
	#endregion

	#region Events
	protected void AgregarEvento(IDomainEvent nuevoEvento) =>
		_eventos.Add(nuevoEvento);

	protected void QuitarEvento(IDomainEvent unEvento) =>
		_eventos.Remove(unEvento);

	public void LiberarEventos() =>
		_eventos.Clear();
	#endregion

	#region Identidad
	/// <summary>
	/// Sin identidad asignada (sólo pasa mientras EF materializa por el ctor privado): no puede
	/// compararse por <see cref="Id"/>, porque todas las transitorias compartirían <c>Guid.Empty</c>.
	/// </summary>
	private bool EsTransitoria() => Id == Guid.Empty;

	public bool Equals(Entity? other) =>
		Equals((object?) other);

	public override bool Equals(object? obj)
	{
		if (obj is not Entity entity)
		{
			return false;
		}

		if (ReferenceEquals(this, entity))
		{
			return true;
		}

		if (EsTransitoria() || entity.EsTransitoria())
		{
			return false;
		}

		return Id.Equals(entity.Id);
	}

	public static bool operator ==(Entity left, Entity right) =>
		Equals(left, right);

	public static bool operator !=(Entity left, Entity right) =>
		!Equals(left, right);

	public override int GetHashCode() =>
		EsTransitoria() ? base.GetHashCode() : Id.GetHashCode();
	#endregion
}