using Domain.Personas;
using Domain.Personas.Domicilios;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Reúne los datos comunes a toda <see cref="Persona"/> (datos personales, domicilio, email y
/// teléfono) para que <see cref="AlumnoBuilder"/> y <see cref="DocenteBuilder"/> los reutilicen.
/// <see cref="Persona"/> es abstracta: este builder no produce una instancia, expone las piezas
/// listas para el constructor del agregado concreto.
/// </summary>
public sealed class PersonaBuilder
{
	#region ESTADO POR DEFECTO
	private string _email = "maria.gonzalez@correo.com";
	private string _telefono = "3814123456";
	#endregion

	#region CONFIGURACIÓN
	public DatosPersonalesBuilder DatosPersonales { get; private set; } = new();

	public DomicilioBuilder Domicilio { get; private set; } = new();

	public string Email => _email;

	public string Telefono => _telefono;

	public PersonaBuilder ConDatosPersonales(DatosPersonalesBuilder datosPersonales)
	{
		DatosPersonales = datosPersonales;
		return this;
	}

	public PersonaBuilder ConDomicilio(DomicilioBuilder domicilio)
	{
		Domicilio = domicilio;
		return this;
	}

	public PersonaBuilder ConEmail(string email)
	{
		_email = email;
		return this;
	}

	public PersonaBuilder ConTelefono(string telefono)
	{
		_telefono = telefono;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public DatosPersonales ConstruirDatosPersonales() =>
		DatosPersonales.Build();

	public Domicilio ConstruirDomicilio() =>
		Domicilio.Build();
	#endregion
}
