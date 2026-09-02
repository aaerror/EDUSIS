using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Personas;

public class Contacto : ValueObject
{
	public TipoContacto TipoContacto { get; private set; }
	public string Descripcion { get; private set; }


	#region CONSTRUCTOR
	public Contacto(TipoContacto tipoContacto, string descripcion)
	{
		if (string.IsNullOrWhiteSpace(descripcion.Trim()))
		{
			throw new ArgumentNullException(nameof(descripcion), "Datos de contacto sin especificar.");
		}

		TipoContacto = tipoContacto;
		Descripcion = descripcion.Trim();
	}

	public static Contacto CrearEmail(string unEmail) =>
		new(TipoContacto.Email, unEmail);

	public static Contacto CrearTelefono(string unTelefono) =>
		new(TipoContacto.Telefono, unTelefono);
	#endregion

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

	public Contacto ModificarContacto(string descripcion)
	{
		return new(TipoContacto, descripcion);
	}

	public override IEnumerable<object> GetEqualityCommponents()
	{
		throw new NotImplementedException();
	}
}