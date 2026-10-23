using Core.ServicioAlumnos;
using Core.ServicioAsistencias;
using Core.ServicioAutenticaciones;
using Core.ServicioCatedras;
using Core.ServicioCursantes;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDocentes;
using Core.ServicioDocumentos;
using Core.ServicioLicencias;
using Core.ServicioMaterias;
using Core.ServicioReportes;
using Core.ServicioUsuarios;
using Core.Shared.Seguridad;
using Core.UnitTests.Infraestructura;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.Shared;

/// <summary>
/// Guardas de autorización de los casos de uso de escritura (plan RBAC, Fase 5, decisión #2:
/// "la interfaz guía y <c>Core</c> garantiza"). Cubre todos los permisos con un caso de uso
/// representativo de cada uno, los bordes de la sesión, y las dos decisiones de alcance que
/// conviene que estén afirmadas y no sólo escritas en el plan: las lecturas quedan abiertas y
/// el login no exige permiso.
/// </summary>
/// <remarks>
/// Todas las invocaciones pasan <c>null!</c> como <c>request</c> a propósito. La guarda es la
/// primera sentencia de cada caso de uso, así que con permiso faltante corta <strong>antes</strong>
/// de mirar el request: si alguien la moviera detrás de la validación de datos, estas pruebas
/// fallarían con <see cref="NullReferenceException"/> en lugar de
/// <c>PermisoDenegadoException</c>. Es decir, afirman la guarda y además su posición.
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class AutorizacionDeCasosDeUsoTests
{
	/// <summary>Host cuyos servicios reciben una sesión autenticada y sin ningún permiso.</summary>
	private static HostDeServicios HostSinPermisos() =>
		new(sesion: SesionDePrueba.SinPermisos());

	/// <summary>Host cuyos servicios reciben una sesión con exactamente los permisos indicados.</summary>
	private static HostDeServicios HostCon(params Permiso[] permisos) =>
		new(sesion: SesionDePrueba.Con(permisos));

	#region Un caso de uso por permiso
	[Fact]
	public async Task RegistrarDocenteAsync_sin_GestionarDocentes_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioDocente>().RegistrarDocenteAsync(null!));
	}

	[Fact]
	public async Task RegistrarAlumnoAsync_sin_GestionarAlumnos_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioAlumno>().RegistrarAlumnoAsync(null!));
	}

	[Fact]
	public async Task RegistrarCurso_sin_GestionarCursos_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioCurso>().RegistrarCurso(null!));
	}

	[Fact]
	public async Task AgregarDivisionAsync_sin_GestionarCursos_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioDivision>().AgregarDivisionAsync(null!));
	}

	[Fact]
	public async Task RegistrarMateriaAsync_sin_GestionarCurriculas_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioMateria>().RegistrarMateriaAsync(null!));
	}

	[Fact]
	public async Task CrearCatedraAsync_sin_GestionarCatedras_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioCatedra>().CrearCatedraAsync(null!));
	}

	[Fact]
	public async Task InscribirCursanteAsync_sin_InscribirCursantes_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioCursante>().InscribirCursanteAsync(null!));
	}

	[Fact]
	public async Task RegistrarCalificacionAsync_sin_CargarCalificaciones_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioCursante>().RegistrarCalificacionAsync(null!));
	}

	[Fact]
	public async Task AbrirPlanillaAsync_sin_TomarAsistencia_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioAsistencia>().AbrirPlanillaAsync(null!));
	}

	[Fact]
	public async Task RegistrarUsuarioAsync_sin_AdministrarUsuarios_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioUsuario>().RegistrarUsuarioAsync(null!));
	}

	[Fact]
	public async Task AsignarRolAsync_sin_AsignarRoles_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioUsuario>().AsignarRolAsync(null!));
	}

	[Fact]
	public async Task SolicitarLicencia_sin_GestionarDocentes_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioLicencia>().SolicitarLicencia(null!));
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegularAsync_sin_GenerarDocumentos_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioDocumento>().GenerarCertificadoAlumnoRegularAsync(null!));
	}

	[Fact]
	public async Task GenerarReporteMatriculasAsync_sin_GenerarReportes_lanza_PermisoDenegado()
	{
		using var host = HostSinPermisos();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioReporte>().GenerarReporteMatriculasAsync(null!));
	}
	#endregion

	#region Bordes de la sesión
	[Fact]
	public async Task Una_sesion_cerrada_no_autoriza_ningun_caso_de_uso_de_escritura()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.Cerrada());

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioCurso>().RegistrarCurso(null!));
	}

	[Fact]
	public async Task Un_permiso_de_otro_modulo_no_alcanza_para_autorizar()
	{
		// Tiene GestionarAlumnos; pide un caso de uso que exige GestionarDocentes.
		using var host = HostCon(Permiso.GestionarAlumnos);

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioDocente>().RegistrarDocenteAsync(null!));
	}

	[Fact]
	public async Task Con_el_permiso_correcto_la_guarda_deja_pasar_y_falla_mas_adelante()
	{
		using var host = HostCon(Permiso.GestionarDocentes);

		// Pasa la autorización y se rompe recién al mirar el request nulo: la guarda no es lo
		// que corta. Afirmar "no lanza PermisoDenegado" y no "no lanza nada" es deliberado.
		var excepcion = await Record.ExceptionAsync(
			() => host.Resolver<IServicioDocente>().RegistrarDocenteAsync(null!));

		excepcion.ShouldNotBeNull();
		excepcion.ShouldNotBeOfType<PermisoDenegadoException>();
	}

	[Fact]
	public async Task El_mensaje_del_permiso_denegado_nombra_la_operacion_y_el_permiso_que_falta()
	{
		using var host = HostSinPermisos();

		var excepcion = await Should.ThrowAsync<PermisoDenegadoException>(
			() => host.Resolver<IServicioDocente>().RegistrarDocenteAsync(null!));

		// La operación la completa [CallerMemberName] en SesionActualExtensions.Exigir.
		excepcion.Message.ShouldContain(nameof(IServicioDocente.RegistrarDocenteAsync));
		excepcion.Message.ShouldContain(Permiso.GestionarDocentes.Descripcion);
	}
	#endregion

	#region Decisiones de alcance de la Fase 5
	[Fact]
	public async Task Los_casos_de_uso_de_lectura_quedan_abiertos_a_proposito()
	{
		// Decisión de alcance escrita en plan/rbac-y-seed.md (Fase 5): la autorización cubre la
		// escritura; las consultas quedan sin guarda porque hay pantallas que leen datos de un
		// módulo ajeno para poblar combos (GestionDivisiones → ListarPreceptoresActivosAsync).
		using var host = HostSinPermisos();

		var cursos = await host.Resolver<IServicioCurso>().ListarCursosAsync();

		cursos.ShouldBeEmpty();
	}

	[Fact]
	public async Task El_login_no_exige_permiso_porque_es_quien_abre_la_sesion()
	{
		// Regresión del deadlock de autorización: ServicioAutenticacion.Login delega en
		// IServicioUsuario.BuscarUsuarioPorIDAsync. Si esa lectura exigiera AdministrarUsuarios,
		// para iniciar sesión haría falta ya tener una.
		using var host = HostSinPermisos();

		var excepcion = await Record.ExceptionAsync(
			() => host.Resolver<IServicioAutenticacion>().Login(null!));

		excepcion.ShouldNotBeOfType<PermisoDenegadoException>();
	}
	#endregion
}
