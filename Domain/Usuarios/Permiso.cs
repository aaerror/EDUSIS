using Domain.Shared;

namespace Domain.Usuarios;

/// <summary>
/// Acción que el sistema puede autorizar. Es el vocabulario con el que la interfaz y los casos
/// de uso preguntan "¿puede hacer esto?", en lugar de preguntar por rol: así la política de
/// autorización vive en un solo lugar (<see cref="Rol"/>) y no se dispersa por cada pantalla.
/// </summary>
/// <remarks>
/// <para>
/// No se persiste: la atribución rol → permisos es código versionado, no datos. Por eso
/// <c>RolesConfiguration</c> ignora <see cref="Rol.Permisos"/>; si no, EF descubriría este tipo
/// como entidad al alcanzarlo desde la navegación y le reclamaría una tabla.
/// </para>
/// <para>
/// Los tres últimos nacieron de partir permisos que antes eran uno solo, porque los diagramas de
/// casos de uso de <c>docs/uml/</c> reparten entre actores distintos lo que una sola atribución
/// concedía junto: <see cref="GestionarPreceptores"/> sale de <see cref="GestionarCursos"/>
/// —CU-04/05/06 son de Secretaría y CU-01/02/03 de Secretaría Académica—,
/// <see cref="GenerarDocumentosAsistencia"/> de <see cref="GenerarDocumentos"/> —CU-30 es el único
/// documento de Preceptoría— y <see cref="GenerarReportesDeCursada"/> de
/// <see cref="GenerarReportes"/> —CU-35/36 contra CU-34/37/38—. La granularidad llega hasta donde
/// hay un actor distinto del otro lado y no más: Secretaría y Secretaría Académica siguen
/// compartiendo <see cref="GenerarDocumentos"/> y <see cref="GenerarReportes"/>.
/// </para>
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
	public static readonly Permiso GenerarDocumentos = new(11, "Generar documentos");
	public static readonly Permiso GenerarReportes = new(12, "Generar reportes");
	public static readonly Permiso GestionarPreceptores = new(13, "Gestionar preceptores");
	public static readonly Permiso GenerarDocumentosAsistencia = new(14, "Generar documentos de asistencia");
	public static readonly Permiso GenerarReportesDeCursada = new(15, "Generar reportes de cursada");


	#region CONSTRUCTOR
	private Permiso()
		: base() { }

	private Permiso(int id, string descripcion)
		: base(id, descripcion) { }
	#endregion
}
