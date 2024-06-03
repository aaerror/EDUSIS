using System.Linq.Expressions;

namespace Domain.Shared
{
    public interface IRepository<TEntity> where TEntity : Entity
    {
        Task AgregarAsync(TEntity entity);
        Task AgregarRango(IEnumerable<TEntity> entities);
        TEntity? BuscarPorID(Guid id);
        IEnumerable<TEntity> BuscarTodos();
        IEnumerable<TEntity> Buscar(Expression<Func<TEntity, bool>> predicate);
        void Modificar(TEntity entity);
        void Eliminar(Guid id);
        void EliminarRango(IEnumerable<TEntity> entities);
    }
}
