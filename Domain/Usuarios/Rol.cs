using Domain.Shared;

namespace Domain.Usuarios;

/// <summary>
/// Rol con el que un usuario entra al sistema. Los cuatro primeros son una jerarquía
/// administrativa; <see cref="Preceptoria"/> y <see cref="SecretariaAcademica"/> son los dos
/// actores que faltaban para que el catálogo se corresponda con los diagramas de casos de uso de
/// <c>docs/uml/</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Preceptoría no es Docente.</strong> El dominio ya conocía la figura como puesto de
/// trabajo (<c>Domain.Docentes.Puestos.Posicion.Preceptor</c>) y <c>Division</c> modela su cargo,
/// pero <c>Posicion</c> es un dato de legajo y no concede un solo permiso: son dos modelos de
/// "quién es esta persona" —el laboral, persistido, y el de seguridad, código versionado— y
/// seguirán separados. Lo que un docente puede hacer lo decide su <see cref="Rol"/> en esta tabla,
/// nunca su <c>Posicion</c>: que <see cref="Docente"/> tenga hoy <see cref="Permiso.TomarAsistencia"/>
/// es atribución de esta tabla, y un docente con puesto de preceptor que entrara con otro rol no la
/// tendría. El puesto sigue sin conceder un solo permiso.
/// </para>
/// <para>
/// <see cref="Direccion"/> se conserva aunque ningún diagrama lo tenga como actor: es un rol de
/// supervisión previsto para más adelante. Mantiene exactamente el alcance que tenía antes de
/// partir los permisos, por eso suma <see cref="Permiso.GestionarPreceptores"/>, que salió de
/// <see cref="Permiso.GestionarCursos"/>, que ya tenía.
/// </para>
/// </remarks>
public class Rol : Enumeration
{
	public static readonly Rol Admin = new(1, "Administrador");
	public static readonly Rol Direccion = new(2, "Dirección");
	public static readonly Rol Docente = new(3, "Docente");
	public static readonly Rol Secretaria = new(4, "Secretaria");
	public static readonly Rol Preceptoria = new(5, "Preceptoría");
	public static readonly Rol SecretariaAcademica = new(6, "Secretaría Académica");

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
			Permiso.GestionarPreceptores,
			Permiso.GestionarCurriculas,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia,
			Permiso.AdministrarUsuarios,
			Permiso.AsignarRoles,
			Permiso.GenerarDocumentos,
			Permiso.GenerarDocumentosAsistencia,
			Permiso.GenerarReportes,
			Permiso.GenerarReportesDeCursada
		},
		[Direccion.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.GestionarCursos,
			Permiso.GestionarPreceptores,
			Permiso.GestionarCurriculas,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia
		},
		// Encargado de Secretaría: CU-04/05/06 (preceptores), CU-28/29 (documentos) y CU-34
		// (reporte de matrículas). GestionarDocentes y GestionarAlumnos no realizan ningún caso
		// del diagrama —el alta de legajos no está modelada como caso de uso— y se conservan
		// porque son las dos únicas secciones del menú que este rol ya sabía abrir.
		[Secretaria.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.GestionarPreceptores,
			Permiso.GenerarDocumentos,
			Permiso.GenerarReportes
		},
		// Encargado de Secretaría Académica: CU-01/02/03 (divisiones), CU-07 a CU-11 (cursantes),
		// CU-17 a CU-27 (cátedras y horarios), CU-31/32/33 (documentos) y CU-37/38 (reportes).
		// Sin GestionarCurriculas: ningún caso del diagrama diseña la currícula, y las consultas
		// que necesita para llegar a las cátedras no exigen permiso.
		[SecretariaAcademica.Id] = new[]
		{
			Permiso.GestionarCursos,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.GenerarDocumentos,
			Permiso.GenerarReportes
		},
		// Encargado de Preceptoría: CU-12 a CU-16 (planillas y toma de asistencia), CU-30
		// (certificado de asistencia) y CU-35/36 (reportes de cursada). CU-26 —consultar
		// horarios, el único caso compartido con Secretaría Académica— es una consulta y no
		// exige permiso.
		//
		// CargarCalificaciones es la única atribución de esta tabla que NO sale de los diagramas:
		// ningún caso de uso modela registrar una nota —los tres diagramas sólo las leen, en CU-33
		// (boletín) y CU-36 (reporte)—, así que no hay actor del que derivarla. Es una decisión de
		// negocio: la planilla de notas la confecciona el docente y la transcribe Preceptoría, que
		// es quien tiene el curso a cargo. Docente la conserva, de modo que el permiso queda
		// compartido y no movido.
		[Preceptoria.Id] = new[]
		{
			Permiso.TomarAsistencia,
			Permiso.CargarCalificaciones,
			Permiso.GenerarDocumentosAsistencia,
			Permiso.GenerarReportesDeCursada
		},
		// Docente tiene CRUD total sobre lo académico: los 15 permisos MENOS AdministrarUsuarios y
		// AsignarRoles. Es una decisión de negocio y NO sale de los diagramas —Docente no es actor
		// de ninguno de los tres—, así que es la segunda atribución de esta tabla que no se deriva
		// de docs/uml/, junto con su CargarCalificaciones.
		//
		// Las dos que NO tiene son el límite deliberado: sin AdministrarUsuarios no crea cuentas y
		// sin AsignarRoles no puede ampliarse sus propios permisos ni los de nadie. Eso es lo que
		// lo mantiene distinto de Admin y lo que mantiene el RBAC gobernable; conceder cualquiera
		// de las dos vuelve el rol equivalente al administrador y esta tabla, decorativa.
		//
		// Consecuencia que conviene tener presente al leer las pruebas: con estos 13, los permisos
		// de Direccion (9), Secretaria (5), SecretariaAcademica (5) y Preceptoria (4) son todos
		// subconjuntos de los de Docente. Las pruebas negativas de los otros roles siguen valiendo
		// —fijan qué NO puede cada uno—, pero ya no hay ninguna asimetría que las respalde acá.
		[Docente.Id] = new[]
		{
			Permiso.GestionarDocentes,
			Permiso.GestionarAlumnos,
			Permiso.GestionarCursos,
			Permiso.GestionarPreceptores,
			Permiso.GestionarCurriculas,
			Permiso.GestionarCatedras,
			Permiso.InscribirCursantes,
			Permiso.CargarCalificaciones,
			Permiso.TomarAsistencia,
			Permiso.GenerarDocumentos,
			Permiso.GenerarDocumentosAsistencia,
			Permiso.GenerarReportes,
			Permiso.GenerarReportesDeCursada
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
