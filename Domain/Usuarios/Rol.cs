using Domain.Shared;

namespace Domain.Usuarios;

public class Rol : Enumeration
{
	public static readonly Rol Admin = new(1, "Administrador");
	public static readonly Rol Direccion = new(2, "Dirección");
	public static readonly Rol Docente = new(3, "Docente");
	public static readonly Rol Secretaria = new(4, "Secretaria");

	/// <summary>
	/// Atribución rol → permisos, indexada por <see cref="Enumeration.Id"/> y NO por instancia.
	/// EF materializa los roles de <c>usuario_rol</c> con el constructor privado sin parámetros,
	/// así que un rol que viene de la base no es la instancia estática: es otro objeto con el
	/// mismo Id. Resolver por Id hace que ambos caminos —el singleton y el materializado—
	/// respondan los mismos permisos; guardarlos como estado de instancia haría que la
	/// autorización negara todo en silencio para cualquier usuario leído de la base.
	/// </summary>
	private static readonly IReadOnlyDictionary<int, Permiso[]> PERMISOS = new Dictionary<int, Permiso[]>
	{
		[Admin.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.GestionarCursos,
			Permiso.GestionarCurriculas,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia,
			Permiso.AdministrarUsuarios,
			Permiso.AsignarRoles,
			Permiso.GenerarDocumentos,
			Permiso.GenerarReportes
		},
		[Direccion.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.GestionarCursos,
			Permiso.GestionarCurriculas,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia
		},
		[Secretaria.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.InscribirCursantes
		},
		[Docente.Id] = new[]
		{
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia
		}
	};


	#region CONSTRUCTOR
	private Rol()
		:base() { }

	private Rol(int value, string displayName)
		: base(value, displayName) { }
	#endregion

	/// <summary>
	/// Permisos del rol. Vacío —y no excepción— para un Id desconocido: un rol que la base
	/// tenga y el código no, no debe habilitar nada.
	/// </summary>
	public IReadOnlyCollection<Permiso> Permisos =>
		PERMISOS.TryGetValue(Id, out var permisos) ? permisos : Array.Empty<Permiso>();

	public bool Tiene(Permiso unPermiso) =>
		Permisos.Contains(unPermiso);
}
