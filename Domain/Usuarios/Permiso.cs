using Domain.Shared;

namespace Domain.Usuarios;

/// <summary>
/// Acción que el sistema puede autorizar. Es el vocabulario con el que la interfaz y los casos
/// de uso preguntan "¿puede hacer esto?", en lugar de preguntar por rol: así la política de
/// autorización vive en un solo lugar (<see cref="Rol"/>) y no se dispersa por cada pantalla.
/// </summary>
/// <remarks>
/// No se persiste: la atribución rol → permisos es código versionado, no datos. Por eso
/// <c>RolesConfiguration</c> ignora <see cref="Rol.Permisos"/>; si no, EF descubriría este tipo
/// como entidad al alcanzarlo desde la navegación y le reclamaría una tabla.
/// </remarks>
public class Permiso : Enumeration
{
	public static readonly Permiso GestionarDocentes = new(1, "Gestionar docentes");
	public static readonly Permiso GestionarAlumnos = new(2, "Gestionar alumnos");
	public static readonly Permiso GestionarCursos = new(3, "Gestionar cursos");
	public static readonly Permiso GestionarCurriculas = new(4, "Gestionar currículas");
	public static readonly Permiso GestionarCatedras = new(5, "Gestionar cátedras");
	public static readonly Permiso InscribirCursantes = new(6, "Inscribir cursantes");
	public static readonly Permiso CargarCalificaciones = new(7, "Cargar calificaciones");
	public static readonly Permiso TomarAsistencia = new(8, "Tomar asistencia");
	public static readonly Permiso AdministrarUsuarios = new(9, "Administrar usuarios");
	public static readonly Permiso AsignarRoles = new(10, "Asignar roles");


	#region CONSTRUCTOR
	private Permiso()
		: base() { }

	private Permiso(int id, string descripcion)
		: base(id, descripcion) { }
	#endregion
}
