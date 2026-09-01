using Domain.Curriculas;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Curricula"/>. Estado por defecto válido: currícula vigente (inicio
/// hoy, sin fin) para un curso arbitrario. <see cref="ConMateria"/> encola materias que
/// <see cref="Build"/> agrega vía <c>Curricula.AgregarMateria</c> (requiere currícula vigente).
/// </summary>
public sealed class CurriculaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _cursoID = Guid.NewGuid();
	private DateTime _fechaInicio = DateTime.Today;
	private DateTime? _fechaFin = null;
	private readonly List<MateriaPendiente> _materias = new();
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

	public CurriculaBuilder ConMateria(string descripcion, int horasCatedra)
	{
		_materias.Add(new MateriaPendiente(descripcion, horasCatedra));
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Curricula Build()
	{
		var curricula = new Curricula(_cursoID, _fechaInicio, _fechaFin);

		foreach (var materia in _materias)
		{
			curricula.AgregarMateria(materia.Descripcion, materia.HorasCatedra);
		}

		return curricula;
	}

	private readonly record struct MateriaPendiente(string Descripcion, int HorasCatedra);
	#endregion
}
