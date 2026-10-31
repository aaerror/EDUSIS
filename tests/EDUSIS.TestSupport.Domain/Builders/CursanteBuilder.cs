using Domain.Cursantes;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Cursante"/>. Recibe el alumno y la división como <see cref="Guid"/>
/// (no depende de <see cref="PersonaBuilder"/> ni de un builder de división). Estado por
/// defecto válido: ciclo lectivo en curso, inicio hoy, no recursante, sin calificaciones.
/// </summary>
public sealed class CursanteBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _divisionID = Guid.NewGuid();
	private Guid _alumnoID = Guid.NewGuid();
	private CicloLectivo _cicloLectivo = new CicloLectivoBuilder().Build();
	private DateTime _fechaInicio = DateTime.Today;
	private bool _esRecursante = false;
	private DateTime? _fechaBaja = null;
	#endregion

	#region CONFIGURACIÓN
	public CursanteBuilder ConDivision(Guid divisionID)
	{
		_divisionID = divisionID;
		return this;
	}

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

	/// <summary>
	/// Inscripción cerrada: es la pieza que permite sembrar el historial de un alumno trasladado
	/// —una inscripción con fecha de fin más la activa en la división destino—.
	/// </summary>
	public CursanteBuilder DadoDeBajaEl(DateTime fechaBaja)
	{
		_fechaBaja = fechaBaja;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Cursante Build()
	{
		var cursante = new Cursante(_divisionID, _alumnoID, _cicloLectivo, _fechaInicio, _esRecursante);

		if (_fechaBaja.HasValue)
		{
			cursante.DarDeBaja(_fechaBaja.Value);
		}

		return cursante;
	}
	#endregion
}
