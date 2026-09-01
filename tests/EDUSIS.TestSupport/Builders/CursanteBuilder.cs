using Domain.Cursantes;
using Domain.Cursantes.Asistencias;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Cursante"/>. Recibe el alumno como <see cref="Guid"/> (no depende de
/// <see cref="PersonaBuilder"/>). Estado por defecto válido: ciclo lectivo en curso, inicio hoy,
/// no recursante, sin asistencias. <see cref="ConAsistencia"/> encola asistencias que
/// <see cref="Build"/> registra vía <c>Cursante.RegistrarAsistencia</c>.
/// </summary>
public sealed class CursanteBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _alumnoID = Guid.NewGuid();
	private CicloLectivo _cicloLectivo = new CicloLectivoBuilder().Build();
	private DateTime _fechaInicio = DateTime.Today;
	private bool _esRecursante = false;
	private readonly List<Asistencia> _asistencias = new();
	#endregion

	#region CONFIGURACIÓN
	public CursanteBuilder ConAlumno(Guid alumnoID)
	{
		_alumnoID = alumnoID;
		return this;
	}

	public CursanteBuilder ConCicloLectivo(CicloLectivo cicloLectivo)
	{
		_cicloLectivo = cicloLectivo;
		return this;
	}

	public CursanteBuilder ConFechaInicio(DateTime fechaInicio)
	{
		_fechaInicio = fechaInicio;
		return this;
	}

	public CursanteBuilder ComoRecursante(bool esRecursante = true)
	{
		_esRecursante = esRecursante;
		return this;
	}

	public CursanteBuilder ConAsistencia(Asistencia asistencia)
	{
		_asistencias.Add(asistencia);
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Cursante Build()
	{
		var cursante = new Cursante(_alumnoID, _cicloLectivo, _fechaInicio, _esRecursante);

		foreach (var asistencia in _asistencias)
		{
			cursante.RegistrarAsistencia(asistencia.Fecha, asistencia.Falta, asistencia.Minutos, asistencia.Observacion);
		}

		return cursante;
	}
	#endregion
}
