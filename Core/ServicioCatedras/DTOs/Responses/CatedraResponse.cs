namespace Core.ServicioCatedras.DTOs.Responses;

/// <summary>
/// Una cátedra (materia en una división) con los nombres de lo que referencia.
/// </summary>
/// <remarks>
/// <see cref="Materia"/>, <see cref="Division"/> y <see cref="DocenteEnFunciones"/> los resuelve
/// <c>Core</c>: el agregado sólo guarda identificadores, y la pantalla los cruzaba en memoria con
/// otros listados. <see cref="DocenteEnFunciones"/> es <c>null</c> si nadie está frente al curso.
/// </remarks>
public record CatedraResponse(
	Guid CatedraID,
	Guid MateriaID,
	Guid DivisionID,
	int? CargaHoraria,
	int HorasAsignadas,
	int HorasSinAsignar,
	Guid? DocenteEnFuncionesID,
	string Materia,
	string Division,
	string? DocenteEnFunciones);
