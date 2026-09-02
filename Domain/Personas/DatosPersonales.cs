using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Personas;

public class DatosPersonales : ValueObject
{
	public string Apellido { get; private set; }
	public string Nombre { get; private set; }
	public string Documento { get; private set; }
	public Sexo Sexo { get; private set; }
	public DateTime FechaNacimiento { get; private set; }
	public string Nacionalidad { get; private set; }


	#region CONSTRUCTOR
	private DatosPersonales(string apellido, string nombre, string documento, Sexo sexo, DateTime fechaNacimiento, string nacionalidad)
	{
		var mensajeError = "La información personal posee datos incompletos.";
		// Corroboramos los datos que se ingresa
		if (string.IsNullOrWhiteSpace(apellido.Trim()))
		{
			throw new ArgumentNullException(nameof(apellido), mensajeError);
		}

		if (string.IsNullOrWhiteSpace(nombre.Trim()))
		{
			throw new ArgumentNullException(nameof(nombre), mensajeError);
		}

		if (string.IsNullOrWhiteSpace(documento.Trim()))
		{
			throw new ArgumentNullException(nameof(documento), mensajeError);
		}

		// Validamos el formato del documento ingresado.
		Regex re = new Regex(@"^(\d{7,8})$");
		if (!re.IsMatch(documento))
		{
			throw new FormatException($"El formato del DNI, { documento }, es inválido. Formatos habilitados: xxxxxxxx");
		}

		/*
		string patron = string.Concat(@"^(2[0347]|3[034])\-?", $"({ dni })", @"\-?(\d)$");
		re = new Regex(patron);
		if (!re.IsMatch(cuil))
		{
			throw new FormatException($"El formato del CUIL, { CUIL }, es inválido. Formatos habilitados: xx-xxxxxxxx-x");
		}
		*/

		if (fechaNacimiento.Date > DateTime.Today.Date)
		{
			throw new ArgumentException($"La fecha de nacimiento no puede ser mayor a la fecha actual ({DateTime.Today.Date}).", nameof(fechaNacimiento));
		}

		if (string.IsNullOrWhiteSpace(nacionalidad.Trim()))
		{
			throw new ArgumentNullException(nameof(nacionalidad), mensajeError);
		}

		Apellido = apellido;
		Nombre = nombre;
		Documento = documento;
		Sexo = sexo;
		EstablecerFechaNacimiento(fechaNacimiento);
		Nacionalidad = nacionalidad;
	}

	public static DatosPersonales Crear(string apellido, string nombre, string dni, string sexo, DateTime fechaNacimiento, string nacionalidad) =>
		new(apellido, nombre, dni, Enum.Parse<Sexo>(sexo), fechaNacimiento, nacionalidad);
	#endregion

	private void EstablecerFechaNacimiento(DateTime fechaNacimiento)
	{
		if (fechaNacimiento.Date > DateTime.Today.Date)
		{
			throw new ArgumentException($"La fecha de nacimiento no puede ser mayor a la fecha actual ({DateTime.Today.Date}).", nameof(fechaNacimiento));
		}

		FechaNacimiento = fechaNacimiento.Date;
	}
	
	internal DatosPersonales CambiarNombreCompleto(string apellido, string nombre)
	{
		return new(apellido, nombre, Documento, Sexo, FechaNacimiento, Nacionalidad);
	}

	internal DatosPersonales CambiarSexo(string apellido, string nombre, string sexo)
	{
		var nuevoSexo = Enum.Parse<Sexo>(sexo);
		if (Sexo.Equals(nuevoSexo))
		{
			throw new ArgumentException("Error al hacer el cambio de sexo: El sexo registrado y el nuevo son iguales.", nameof(nuevoSexo));
		}

		return new(apellido, nombre, Documento, nuevoSexo, FechaNacimiento, Nacionalidad);
	}

	public string NombreCompleto() => $"{Apellido}, {Nombre}";
	
	public int Edad() => DateTime.Today.Year - FechaNacimiento.Date.Year;

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Apellido;
		yield return Nombre;
		yield return Sexo;
		yield return FechaNacimiento;
		yield return Nacionalidad;
	}
}