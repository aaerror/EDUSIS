using Domain.Shared;

namespace Domain.Divisiones;

public interface IDivisionRepository : IRepository<Division>
{
	Task<bool> ExisteDivisionConDescripcionAsync(Guid cursoID, string descripcion);
	Task<bool> ExistePreceptorAsignadoAsync(Guid docenteID);

	/// <summary>
	/// Las divisiones que tienen a ese docente como preceptor. Es la versión materializada de
	/// <see cref="ExistePreceptorAsignadoAsync"/>, que sólo responde si hay alguna.
	/// </summary>
	/// <remarks>
	/// La consume el borrado del docente, que necesita las instancias para vaciarles el cargo con
	/// <c>Division.QuitarPreceptor()</c> — un método del dominio con su propia invariante— en lugar
	/// de poner la columna en <c>NULL</c> por detrás.
	/// </remarks>
	Task<IReadOnlyCollection<Division>> BuscarPorPreceptorAsync(Guid docenteID);
	Task<IReadOnlyCollection<string>> DescripcionesDelCursoAsync(Guid cursoID);

	/// <summary>Letra de cada división pedida. Un identificador que no existe no aparece en el resultado.</summary>
	Task<IReadOnlyDictionary<Guid, string>> DescripcionesAsync(IEnumerable<Guid> divisionesID);
	Task<IReadOnlyCollection<Division>> DivisionesDelCursoAsync(Guid cursoID);
	Task<Division?> BuscarPorCursoYDescripcionAsync(Guid cursoID, string descripcion);
}
