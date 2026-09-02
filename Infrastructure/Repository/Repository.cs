using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repository;

public class Repository<TEntity> : IRepository<TEntity>
	where TEntity : Entity
{
	protected readonly DbContext Context;


	#region CONSTRUCTOR
	public Repository(DbContext context)
	{
		Context = context;
	}
	#endregion

	public async Task AgregarAsync(TEntity entity) =>
		await Context.AddAsync<TEntity>(entity);

	public async Task AgregarRangoAsync(IEnumerable<TEntity> entities) =>
		await Context.AddRangeAsync(entities);

	public async virtual Task<TEntity?> BuscarPorIDAsync(params object[] ids) =>
		await Context.Set<TEntity>().FindAsync(ids);

	public async Task<IEnumerable<TEntity>> BuscarTodosAsync() =>
		await Context.Set<TEntity>().ToListAsync();

	public async Task<IEnumerable<TEntity>> BuscarAsync(Expression<Func<TEntity, bool>> predicate) =>
		await Context.Set<TEntity>()
			.Where(predicate)
			.ToListAsync();

	public virtual void Modificar(TEntity entity) => 
		Context.Update(entity);

	public void ModificarRango(IEnumerable<TEntity> entities) =>
		Context.UpdateRange(entities);

	public async Task Eliminar(params object[] ids)
	{
		var entity = await BuscarPorIDAsync(ids);
		if (entity is not null)
		{
			Context.Remove(entity);
		}
	}

	public void EliminarRango(IEnumerable<TEntity> entities) =>
		Context.RemoveRange(entities);
}