using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public abstract class Repository<TEntity> : IRepository<TEntity>
	where TEntity : Entity
{
	protected readonly DbContext Context;

	#region CONSTRUCTOR
	protected Repository(DbContext context)
	{
		Context = context;
	}
	#endregion

	protected virtual IQueryable<TEntity> Consulta() => Context.Set<TEntity>();

	public async Task AgregarAsync(TEntity entidad) =>
		await Context.AddAsync<TEntity>(entidad);

	public async Task AgregarRangoAsync(IEnumerable<TEntity> entidades) =>
		await Context.AddRangeAsync(entidades);

	public async Task<TEntity?> BuscarPorIDAsync(Guid id) =>
		await Consulta().FirstOrDefaultAsync(x => x.Id == id);

	public async Task<IReadOnlyCollection<TEntity>> BuscarTodosAsync() =>
		await Consulta().ToListAsync();

	public virtual void Modificar(TEntity entidad) =>
		Context.Update(entidad);

	public void ModificarRango(IEnumerable<TEntity> entidades) =>
		Context.UpdateRange(entidades);

	public async Task EliminarAsync(Guid id)
	{
		var entidad = await BuscarPorIDAsync(id);
		if (entidad is not null)
		{
			Context.Remove(entidad);
		}
	}

	public void EliminarRango(IEnumerable<TEntity> entidades) =>
		Context.RemoveRange(entidades);
}