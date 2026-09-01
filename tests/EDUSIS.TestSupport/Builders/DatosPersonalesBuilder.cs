using Domain.Personas;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="DatosPersonales"/>. Estado por defecto válido, con
/// acentos y <c>ñ</c> para no enmascarar problemas de codificación en las suites de
/// persistencia. <see cref="Build"/> usa la factoría pública <see cref="DatosPersonales.Crear"/>;
/// si los datos configurados violan una invariante, deja propagar la excepción de dominio.
/// </summary>
public sealed class DatosPersonalesBuilder
{
	#region ESTADO POR DEFECTO
	private string _apellido = "Ñáñez";
	private string _nombre = "José María";
	private string _documento = "30111222";
	private Sexo _sexo = Sexo.Femenino;
	private DateTime _fechaNacimiento = new(1990, 6, 15);
	private string _nacionalidad = "Argentina";
	#endregion

	#region CONFIGURACIÓN
	public DatosPersonalesBuilder ConApellido(string apellido)
	{
		_apellido = apellido;
		return this;
	}

	public DatosPersonalesBuilder ConNombre(string nombre)
	{
		_nombre = nombre;
		return this;
	}

	public DatosPersonalesBuilder ConApellidoYNombre(string apellido, string nombre)
	{
		_apellido = apellido;
		_nombre = nombre;
		return this;
	}

	public DatosPersonalesBuilder ConDocumento(string documento)
	{
		_documento = documento;
		return this;
	}

	public DatosPersonalesBuilder ConSexo(Sexo sexo)
	{
		_sexo = sexo;
		return this;
	}

	public DatosPersonalesBuilder ConFechaNacimiento(DateTime fechaNacimiento)
	{
		_fechaNacimiento = fechaNacimiento;
		return this;
	}

	/// <summary>Fija la fecha de nacimiento de modo que <c>DatosPersonales.Edad()</c> devuelva <paramref name="edad"/>.</summary>
	public DatosPersonalesBuilder ConEdad(int edad)
	{
		_fechaNacimiento = DateTime.Today.AddYears(-edad);
		return this;
	}

	public DatosPersonalesBuilder ConNacionalidad(string nacionalidad)
	{
		_nacionalidad = nacionalidad;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public DatosPersonales Build() =>
		DatosPersonales.Crear(_apellido, _nombre, _documento, _sexo.ToString(), _fechaNacimiento, _nacionalidad);
	#endregion
}
