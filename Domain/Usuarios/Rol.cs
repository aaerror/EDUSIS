using Domain.Shared;

namespace Domain.Usuarios;

public class Rol : Enumeration
{
	public static readonly Rol Admin = new(1, "Administrador");
	public static readonly Rol Direccion = new(2, "Dirección");
	public static readonly Rol Docente = new(3, "Docente");
	public static readonly Rol Secretaria = new(4, "Secretaria");


	private Rol()
		:base() { }

	private Rol(int value, string displayName)
		: base(value, displayName) { }
}