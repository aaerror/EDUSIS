namespace Domain.Usuarios.Exceptions;

/// <summary>
/// Guarda de último recurso del sistema: no se puede eliminar la única cuenta con rol
/// <see cref="Rol.Admin"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>AdministrarUsuarios</c> y <c>AsignarRoles</c> son los dos permisos que separan a
/// <see cref="Rol.Admin"/> de <see cref="Rol.Docente"/>, que tiene los otros trece. Sin ninguna
/// cuenta que los lleve, nadie puede crear usuarios ni asignar roles: el sistema queda sin forma de
/// recuperar su propia administración desde la aplicación, y el único camino de vuelta es tocar la
/// base a mano o volver a correr el seeder.
/// </para>
/// <para>
/// Por eso la regla no vive en la pantalla sino acá abajo: la misma operación llamada desde el
/// seeder, desde una prueba o desde un caso de uso futuro tiene que chocar con lo mismo.
/// </para>
/// </remarks>
public class UltimoAdministradorException : Exception
{
	private const string ERROR = "No se puede eliminar la última cuenta con rol Administrador: el sistema quedaría sin forma de administrar usuarios ni roles.";


	public UltimoAdministradorException()
		: base(ERROR) { }
	public UltimoAdministradorException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public UltimoAdministradorException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
