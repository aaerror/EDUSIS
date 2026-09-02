using Domain.Personas;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="Contacto"/>. Por defecto produce un contacto de tipo
/// <see cref="TipoContacto.Email"/> válido. <see cref="Build"/> usa el constructor público.
/// </summary>
public sealed class ContactoBuilder
{
	#region ESTADO POR DEFECTO
	private TipoContacto _tipoContacto = TipoContacto.Email;
	private string _descripcion = "maria.gonzalez@correo.com";
	#endregion

	#region CONFIGURACIÓN
	public ContactoBuilder ConTipo(TipoContacto tipoContacto)
	{
		_tipoContacto = tipoContacto;
		return this;
	}

	public ContactoBuilder ConDescripcion(string descripcion)
	{
		_descripcion = descripcion;
		return this;
	}

	public ContactoBuilder ComoEmail(string email)
	{
		_tipoContacto = TipoContacto.Email;
		_descripcion = email;
		return this;
	}

	public ContactoBuilder ComoTelefono(string telefono)
	{
		_tipoContacto = TipoContacto.Telefono;
		_descripcion = telefono;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Contacto Build() =>
		new(_tipoContacto, _descripcion);
	#endregion
}
