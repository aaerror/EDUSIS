using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repository;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    protected readonly DbContext Context;


    public Repository(DbContext context)
    {
        Context = context;
    }

    public async Task AgregarAsync(TEntity entity) => await Context.AddAsync<TEntity>(entity);

    public async Task AgregarRango(IEnumerable<TEntity> entities) => await Context.AddRangeAsync(entities);

    public virtual TEntity? BuscarPorID(Guid id) => Context.Set<TEntity>().Find(id);

    public IEnumerable<TEntity> BuscarTodos() => Context.Set<TEntity>().ToList();

    public IEnumerable<TEntity> Buscar(Expression<Func<TEntity, bool>> predicate) => Context.Set<TEntity>().Where(predicate).ToList();

    public virtual void Modificar(TEntity entity) => Context.Update(entity);

    public void Eliminar(Guid id)
    {
        var entity = BuscarPorID(id);
        Context.Remove(entity);
    }

    public void EliminarRango(IEnumerable<TEntity> entities) => Context.RemoveRange(entities);
}
