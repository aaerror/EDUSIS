namespace Domain.Shared;

public interface IRepository<TEntity>
	where TEntity : Entity
{
	Task AgregarAsync(TEntity entidad);
	Task AgregarRangoAsync(IEnumerable<TEntity> entidades);
	Task<TEntity?> BuscarPorIDAsync(Guid id);
	Task<IReadOnlyCollection<TEntity>> BuscarTodosAsync();
	void Modificar(TEntity entidad);
	void ModificarRango(IEnumerable<TEntity> entidades);
	Task EliminarAsync(Guid id);
	void EliminarRango(IEnumerable<TEntity> entidades);
}