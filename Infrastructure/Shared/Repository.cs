using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Shared;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    protected readonly DbContext Context;


    public Repository(DbContext context)
    {
        Context = context;
    }

    public void Agregar(TEntity entity) => Context.Add<TEntity>(entity);

    public virtual TEntity BuscarPorID(Guid id) => Context.Set<TEntity>().Find(id);

    public IEnumerable<TEntity> BuscarTodos() => Context.Set<TEntity>().ToList();

    public IEnumerable<TEntity> Buscar(Expression<Func<TEntity, bool>> predicate) => Context.Set<TEntity>().Where(predicate).ToList();

    public virtual void ActualizarDatos(TEntity entity) => Context.Update<TEntity>(entity);

    public void BorrarDatos(Guid id)
    {
        var entity = BuscarPorID(id);
        Context.Remove(entity);
    }
}
