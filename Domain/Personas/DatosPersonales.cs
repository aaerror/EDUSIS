using Domain.Personas.Exceptions;
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
	private DatosPersonales(
		string apellido,
		string nombre,
		string documento,
		Sexo sexo,
		DateTime fechaNacimiento,
		string nacionalidad)
	{
		// Sin .Trim() previo: sobre un null lanzaba NullReferenceException antes de llegar a la guarda.
		if (string.IsNullOrWhiteSpace(apellido))
		{
			throw new ApellidoRequeridoException();
		}

		if (string.IsNullOrWhiteSpace(nombre))
		{
			throw new NombreRequeridoException();
		}

		if (string.IsNullOrWhiteSpace(documento))
		{
			throw new DocumentoRequeridoException();
		}

		// Validamos el formato del documento ingresado.
		Regex re = new Regex(@"^(\d{7,8})$");
		if (!re.IsMatch(documento))
		{
			throw new DocumentoInvalidoException(documento);
		}

		/*
		string patron = string.Concat(@"^(2[0347]|3[034])\-?", $"({ dni })", @"\-?(\d)$");
		re = new Regex(patron);
		if (!re.IsMatch(cuil))
		{
			throw new FormatException($"El formato del CUIL, { CUIL }, es inválido. Formatos habilitados: xx-xxxxxxxx-x");
		}
		*/

		if (string.IsNullOrWhiteSpace(nacionalidad))
		{
			throw new NacionalidadRequeridaException();
		}

		Apellido = apellido;
		Nombre = nombre;
		Documento = documento;
		Sexo = sexo;
		EstablecerFechaNacimiento(fechaNacimiento);
		Nacionalidad = nacionalidad;
	}

	public static DatosPersonales Crear(
		string apellido,
		string nombre,
		string dni,
		string sexo,
		DateTime fechaNacimiento,
		string nacionalidad) =>
			new(apellido,
				nombre,
				dni,
				Enum.Parse<Sexo>(sexo),
				fechaNacimiento,
				nacionalidad);
	#endregion

	private void EstablecerFechaNacimiento(DateTime fechaNacimiento)
	{
		if (fechaNacimiento.Date > DateTime.Today.Date)
		{
			throw new FechaNacimientoFuturaException(fechaNacimiento.ToString("d"));
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
			throw new SexoSinCambiosException(sexo);
		}

		return new(apellido, nombre, Documento, nuevoSexo, FechaNacimiento, Nacionalidad);
	}

	public string NombreCompleto() => $"{Apellido}, {Nombre}";
	
	/// <summary>
	/// Edad cumplida al día de hoy a partir de una fecha de nacimiento.
	/// </summary>
	/// <remarks>
	/// <strong>Es estática y pública para que haya una sola implementación de este cálculo.</strong>
	/// Calcular la edad como días transcurridos / 365 es un defecto que no se ve a simple vista —la
	/// edad se adelanta alrededor de un día cada cuatro años—, y
	/// cualquier capa que necesite la edad sin tener el agregado delante tiene que pasar por este
	/// método en lugar de rehacer la resta. Hoy la necesita la pantalla de alta de alumno, que
	/// compara la edad contra el grado con `Domain.Cursos.EdadEscolar` mientras el alumno todavía
	/// no existe como entidad.
	/// </remarks>
	public static int Edad(DateTime fechaNacimiento)
	{
		var hoy = DateTime.Today;
		var edad = hoy.Year - fechaNacimiento.Year;

		// Si el cumpleaños de este año todavía no llegó, no cumplió años.
		// Un nacido el 29/02 cumple el 28/02 en años no bisiestos (AddYears).
		if (fechaNacimiento.Date.AddYears(edad) > hoy)
		{
			edad--;
		}

		return edad;
	}

	public int Edad() => Edad(FechaNacimiento);

	public bool EsMayorDeEdad() => Edad() >= 18;

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Apellido;
		yield return Nombre;
		yield return Sexo;
		yield return FechaNacimiento;
		yield return Nacionalidad;
	}
}