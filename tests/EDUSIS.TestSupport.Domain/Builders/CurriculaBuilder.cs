using Domain.Curriculas;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Curricula"/>. Estado por defecto válido: currícula vigente (inicio
/// hoy, sin fin) para un curso arbitrario. Las materias ya no son responsabilidad de
/// <c>Curricula</c> (agregado propio <c>Domain.Materias.Materia</c>): usar
/// <see cref="MateriaBuilder"/> y asociarla por <c>CurriculaID</c>.
/// </summary>
public sealed class CurriculaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _cursoID = Guid.NewGuid();
	private DateTime _fechaInicio = DateTime.Today;
	private DateTime? _fechaFin = null;
	#endregion

	#region CONFIGURACIÓN
	public CurriculaBuilder ConCurso(Guid cursoID)
	{
		_cursoID = cursoID;
		return this;
	}

	public CurriculaBuilder ConFechaInicio(DateTime fechaInicio)
	{
		_fechaInicio = fechaInicio;
		return this;
	}

	public CurriculaBuilder ConFechaFin(DateTime? fechaFin)
	{
		_fechaFin = fechaFin;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Curricula Build() =>
		new(_cursoID, _fechaInicio, _fechaFin);
	#endregion
}
