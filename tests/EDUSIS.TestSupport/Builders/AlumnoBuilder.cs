using Domain.Alumnos;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Alumno"/>. Estado por defecto válido (persona adulta joven, legajo
/// arbitrario). <see cref="Build"/> usa el constructor público; el <c>Alumno</c> resultante
/// llega con el evento <c>AlumnoInscriptoDomainEvent</c> encolado, tal como en producción.
/// </summary>
public sealed class AlumnoBuilder
{
	#region ESTADO POR DEFECTO
	private PersonaBuilder _persona = new PersonaBuilder()
		.ConDatosPersonales(new DatosPersonalesBuilder().ConEdad(16));
	private string _legajo = "A-000123";
	#endregion

	#region CONFIGURACIÓN
	public AlumnoBuilder ConPersona(PersonaBuilder persona)
	{
		_persona = persona;
		return this;
	}

	public AlumnoBuilder ConDatosPersonales(DatosPersonalesBuilder datosPersonales)
	{
		_persona.ConDatosPersonales(datosPersonales);
		return this;
	}

	public AlumnoBuilder ConDomicilio(DomicilioBuilder domicilio)
	{
		_persona.ConDomicilio(domicilio);
		return this;
	}

	public AlumnoBuilder ConEmail(string email)
	{
		_persona.ConEmail(email);
		return this;
	}

	public AlumnoBuilder ConTelefono(string telefono)
	{
		_persona.ConTelefono(telefono);
		return this;
	}

	public AlumnoBuilder ConLegajo(string legajo)
	{
		_legajo = legajo;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Alumno Build() =>
		new(_legajo, _persona.ConstruirDatosPersonales(), _persona.ConstruirDomicilio(), _persona.Email, _persona.Telefono);
	#endregion
}
