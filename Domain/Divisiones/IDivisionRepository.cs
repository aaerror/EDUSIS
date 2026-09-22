using Domain.Shared;

namespace Domain.Divisiones;

public interface IDivisionRepository : IRepository<Division>
{
	Task<bool> ExisteDivisionConDescripcionAsync(Guid cursoID, string descripcion);
	Task<bool> ExistePreceptorAsignadoAsync(Guid docenteID);
	Task<IReadOnlyCollection<string>> DescripcionesDelCursoAsync(Guid cursoID);
	Task<IReadOnlyCollection<Division>> DivisionesDelCursoAsync(Guid cursoID);
	Task<Division?> BuscarPorCursoYDescripcionAsync(Guid cursoID, string descripcion);
}
