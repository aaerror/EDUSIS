using Domain.Personas.Domicilios;
using Domain.Personas.Exceptions;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Personas;

public abstract class Persona : Entity
{
	public DatosPersonales DatosPersonales { get; private set; }
	public Domicilio Domicilio { get; private set; }
	public string Email { get; private set; }
	public string Telefono { get; private set; }


	#region CONSTRUCTOR
	protected Persona()
		: base() {}

	private Persona(Guid personaId)
		: base(personaId) {}

	private Persona(Guid personaId, DatosPersonales datosPersonales, Domicilio unDomicilio, string email, string telefono)
		: this(personaId)
	{
		Id = personaId;
		DatosPersonales = datosPersonales;
		Domicilio = unDomicilio;
		CargarEmail(email);
		CargarTelefono(telefono);
	}

	public Persona(DatosPersonales datosPersonales, Domicilio unDomicilio, string email, string telefono)
		: this(Guid.NewGuid(), datosPersonales, unDomicilio, email, telefono)
	{
		if (datosPersonales is null)
		{
			throw new ArgumentNullException(nameof(datosPersonales), "Datos personales inexistentes.");
		}

		if (unDomicilio is null)
		{
			throw new ArgumentNullException(nameof(unDomicilio), "Domicilio inexistentes.");
		}

		if (string.IsNullOrWhiteSpace(email))
		{
			throw new ArgumentNullException(nameof(email), "Email inexistente.");
		}

		if (string.IsNullOrWhiteSpace(telefono))
		{
			throw new ArgumentNullException(nameof(telefono), "Telefono inexistente.");
		}
	}
	#endregion

	#region InformacionPersonal
	public void CambiarNombreCompleto(string apellido, string nombre)
	{
		DatosPersonales = DatosPersonales.CambiarNombreCompleto(apellido, nombre);
	}

	public void CambiarSexo(string apellido, string nombre, string sexo)
	{
		DatosPersonales = DatosPersonales.CambiarSexo(apellido, nombre, sexo);
	}
	#endregion

	#region Domicilio
	public void CambiarDomicilio(Domicilio unDomicilio)
	{
		if (unDomicilio is null)
		{
			throw new ArgumentNullException(nameof(unDomicilio), "Datos del nuevo domicilio incompletos.");
		}

		Domicilio = unDomicilio;
	}

	public void CambiarDireccion(Direccion nuevaDireccion)
	{
		Domicilio = Domicilio.CambiarDireccion(nuevaDireccion);
	}
	#endregion

	#region Contacto
	private bool EsEmailValido(string unEmail)
	{
		bool esValido = false;

		/**
		 * Usamos regular expression para validar el formato del email.
		 * https://mailtrap.io/blog/validate-email-address-c/
		 */
		Regex re = new Regex(@"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$");
		if (re.IsMatch(unEmail))
		{
			esValido = true;
		}

		return esValido;
	}

	private void CargarEmail(string unEmail)
	{
		if (!EsEmailValido(unEmail))
		{
			throw new FormatoEmailInvalidoException();
		}

		Email = unEmail;
	}

	private bool EsTelefonoValido(string unTelefono)
	{
		bool esValido = false;

		/**
		 * Usamos regular expression para validar el formato del número de teléfono
		 * 
		 * https://es.stackoverflow.com/questions/136325/validar-tel%C3%A9fonos-de-argentina-con-una-expresi%C3%B3n-regular
		 * 
		 * Toma como opcionales:
		 *      el prefijo internacional (54)
		 *      el prefijo internacional para celulares (9)
		 *      el prefijo de acceso a interurbanas (0)
		 *      el prefijo local para celulares (15)
		 * Es obligatorio:
		 *       el código de área (11, 2xx, 2xxx, 3xx, 3xxx, 6xx y 8xx)
		 *       (no toma como válido un número local sin código de área como 4444-0000)
		 **/
		Regex re = new Regex(@"^(?:(?:00)?549?)?0?(?:11|[2368]\d)(?:(?=\d{0,2}15)\d{2})??\d{8}$");
		if (re.IsMatch(unTelefono))
		{
			esValido = true;
		}

		return esValido;
	}

	private void CargarTelefono(string unTelefono)
	{
		if (!EsTelefonoValido(unTelefono))
		{
			throw new FormatoTelefonoInvalidoException();
		}

		Telefono = unTelefono;
	}

	public void CambiarContacto(string unEmail, string unTelefono)
	{
		CargarEmail(unEmail);
		CargarTelefono(unTelefono);
	}
	#endregion
}