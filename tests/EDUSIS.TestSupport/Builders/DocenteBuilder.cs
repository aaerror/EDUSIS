using Domain.Docentes;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Docente"/>. Estado por defecto válido: legajo de 6 dígitos, CUIL con
/// prefijo válido, alta hace tres años y persona en edad activa (ni menor de 18 ni en edad
/// jubilatoria). <see cref="ConPuesto"/> encola cargos docentes que <see cref="Build"/> aplica
/// vía <c>Docente.AsignarCargoDocente</c> después de construir el agregado.
/// </summary>
public sealed class DocenteBuilder
{
	#region ESTADO POR DEFECTO
	private PersonaBuilder _persona = new PersonaBuilder()
		.ConDatosPersonales(new DatosPersonalesBuilder().ConEdad(38));
	private string _legajo = "100200";
	private string _cuil = "20301112229";
	private DateTime _fechaAlta = DateTime.Today.AddYears(-3);
	private readonly List<PuestoPendiente> _puestos = new();
	#endregion

	#region CONFIGURACIÓN
	public DocenteBuilder ConPersona(PersonaBuilder persona)
	{
		_persona = persona;
		return this;
	}

	public DocenteBuilder ConDatosPersonales(DatosPersonalesBuilder datosPersonales)
	{
		_persona.ConDatosPersonales(datosPersonales);
		return this;
	}

	public DocenteBuilder ConDomicilio(DomicilioBuilder domicilio)
	{
		_persona.ConDomicilio(domicilio);
		return this;
	}

	public DocenteBuilder ConEmail(string email)
	{
		_persona.ConEmail(email);
		return this;
	}

	public DocenteBuilder ConTelefono(string telefono)
	{
		_persona.ConTelefono(telefono);
		return this;
	}

	public DocenteBuilder ConLegajo(string legajo)
	{
		_legajo = legajo;
		return this;
	}

	public DocenteBuilder ConCuil(string cuil)
	{
		_cuil = cuil;
		return this;
	}

	public DocenteBuilder ConFechaAlta(DateTime fechaAlta)
	{
		_fechaAlta = fechaAlta;
		return this;
	}

	/// <summary>
	/// Encola un cargo docente. Por defecto <c>Profesor</c> / <c>Pendiente</c> con inicio hoy y
	/// sin fecha de fin. <c>Docente.AsignarCargoDocente</c> exige que el inicio no sea anterior
	/// al día de la fecha ni al alta del docente.
	/// </summary>
	public DocenteBuilder ConPuesto(string posicion = "Profesor", string estado = "Pendiente", DateTime? fechaInicio = null, DateTime? fechaFin = null)
	{
		_puestos.Add(new PuestoPendiente(posicion, estado, fechaInicio ?? DateTime.Today, fechaFin));
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Docente Build()
	{
		var docente = new Docente(_legajo, _cuil, _fechaAlta, _persona.ConstruirDatosPersonales(), _persona.ConstruirDomicilio(), _persona.Email, _persona.Telefono);

		foreach (var puesto in _puestos)
		{
			docente.AsignarCargoDocente(puesto.Posicion, puesto.Estado, puesto.FechaInicio, puesto.FechaFin);
		}

		return docente;
	}

	private readonly record struct PuestoPendiente(string Posicion, string Estado, DateTime FechaInicio, DateTime? FechaFin);
	#endregion
}
