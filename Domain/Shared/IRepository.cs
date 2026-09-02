using System.Linq.Expressions;

namespace Domain.Shared;

public interface IRepository<TEntity>
	where TEntity : Entity
{
	Task AgregarAsync(TEntity entity);
	Task AgregarRangoAsync(IEnumerable<TEntity> entities);
	Task<TEntity?> BuscarPorIDAsync(params object[] ids);
	Task<IEnumerable<TEntity>> BuscarTodosAsync();
	Task<IEnumerable<TEntity>> BuscarAsync(Expression<Func<TEntity, bool>> predicate);
	void Modificar(TEntity entity);
	void ModificarRango(IEnumerable<TEntity> entities);
	Task Eliminar(params object[] ids);
	void EliminarRango(IEnumerable<TEntity> entities);
}