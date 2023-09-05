using System.Linq.Expressions;

namespace Domain.Shared
{
    public interface IRepository<TEntity> where TEntity : Entity
    {
        void Agregar(TEntity entity);
        TEntity BuscarPorID(Guid id);
        IEnumerable<TEntity> BuscarTodos();
        IEnumerable<TEntity> Buscar(Expression<Func<TEntity, bool>> predicate);
        void ActualizarDatos(TEntity entity);
        void BorrarDatos(Guid id);
    }
}
