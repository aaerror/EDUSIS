using Core.ServicioReportes;
using Core.ServicioReportes.DTOs.Requests;
using Core.Shared.Documentos;
using Core.Shared.Seguridad;
using Core.UnitTests.Infraestructura;
using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Materias;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioReportes;

/// <summary>
/// <see cref="IServicioReporte"/>: los cinco reportes institucionales. Las pruebas afirman sobre
/// el <see cref="ReporteTabularDocumento"/> que recibió el puerto de renderizado, es decir sobre
/// las columnas y las filas que el caso de uso calculó.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioReporteTests
{
	private const string CICLO = "2026";

	private readonly HostDeServicios _host = new();
	private readonly IServicioReporte _servicio;

	private Curso _curso = null!;
	private Division _division = null!;

	/// <summary>Materias sembradas, ordenadas por descripción igual que las informan los reportes.</summary>
	private readonly List<Materia> _materias = new();

	public ServicioReporteTests()
	{
		_servicio = _host.Resolver<IServicioReporte>();
	}

	#region Siembra
	/// <summary>Curso, división y currícula vigente compartidos por las inscripciones de la prueba.</summary>
	private void SembrarEstructura(params string[] materias)
	{
		_curso = new CursoBuilder().ConGrado("Tercero").ConNivelEducativo("Secundaria").Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(_curso);

		_division = new DivisionBuilder().ConCurso(_curso.Id).ConDescripcion("B").Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(_division);

		var curricula = new CurriculaBuilder().ConCurso(_curso.Id).ConFechaFin(null).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);

		foreach (var descripcion in materias.OrderBy(x => x))
		{
			var materia = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion(descripcion).Build();
			_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);
			_materias.Add(materia);
		}
	}

	private (Alumno Alumno, Cursante Cursante) SembrarInscripcion(string legajo, bool recursante = false)
	{
		var alumno = new AlumnoBuilder().ConLegajo(legajo).Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);

		var cursante = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(_division.Id)
			.ConCicloLectivo(CicloLectivo.Crear(CICLO))
			.ComoRecursante(recursante)
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		return (alumno, cursante);
	}

	private static ReportePorCicloRequest TodaLaInstitucion() =>
		new(CICLO, null);
	#endregion

	#region Matrículas
	[Fact]
	public async Task GenerarReporteMatriculas_emite_una_fila_por_inscripcion()
	{
		SembrarEstructura();
		var primero = SembrarInscripcion("AL0001");
		var segundo = SembrarInscripcion("AL0002", recursante: true);

		await _servicio.GenerarReporteMatriculasAsync(TodaLaInstitucion());

		var reporte = _host.Generador.Reporte.ShouldNotBeNull();
		reporte.Titulo.ShouldBe("Reporte de matrículas");
		reporte.Filas.Count.ShouldBe(2);
		reporte.Columnas.ShouldContain("Legajo");

		// La condición distingue al recursante del regular: es el dato por el que se emite el
		// reporte de matrículas.
		reporte.Filas.Select(fila => fila.Last()).ShouldBe(new[] { "Regular", "Recursante" }, ignoreOrder: true);
	}

	[Fact]
	public async Task GenerarReporteMatriculas_declara_que_abarca_la_institucion_completa()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		await _servicio.GenerarReporteMatriculasAsync(TodaLaInstitucion());

		var reporte = _host.Generador.Reporte.ShouldNotBeNull();
		reporte.Alcance.ShouldContain(CICLO);
		reporte.Alcance.ShouldContain("Institución completa");
	}

	[Fact]
	public async Task GenerarReporteMatriculas_acotado_a_una_division_declara_curso_y_division()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		await _servicio.GenerarReporteMatriculasAsync(new ReportePorCicloRequest(CICLO, _division.Id));

		var reporte = _host.Generador.Reporte.ShouldNotBeNull();
		reporte.Alcance.ShouldContain("3° Año B");
	}

	[Fact]
	public async Task GenerarReporteMatriculas_sin_inscripciones_en_el_alcance_falla()
	{
		SembrarEstructura();

		// Un reporte de cero filas no es un reporte: el operador se equivocó de ciclo o de
		// división, y conviene decírselo en lugar de entregarle una hoja en blanco.
		var ex = await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.GenerarReporteMatriculasAsync(TodaLaInstitucion()));

		ex.Message.ShouldContain(CICLO);
		_host.Generador.Invocaciones.ShouldBe(0);
	}

	[Fact]
	public async Task GenerarReporteMatriculas_sin_permiso_no_llega_al_generador()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.SinPermisos());
		var servicio = host.Resolver<IServicioReporte>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.GenerarReporteMatriculasAsync(TodaLaInstitucion()));

		host.Generador.Invocaciones.ShouldBe(0);
	}

	[Fact]
	public async Task GenerarReporteMatriculas_con_permiso_de_documentos_no_alcanza()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.Con(Permiso.GenerarDocumentos));
		var servicio = host.Resolver<IServicioReporte>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.GenerarReporteMatriculasAsync(TodaLaInstitucion()));
	}
	#endregion

	#region Asistencias
	[Fact]
	public async Task GenerarReporteAsistencias_computa_cada_tipo_y_el_porcentaje()
	{
		SembrarEstructura();
		var inscripcion = SembrarInscripcion("AL0001");

		// Cuatro jornadas: presente, tardanza, ausencia e inasistencia. La tardanza cuenta como
		// concurrencia, así que la asistencia es 2 de 4.
		SembrarPlanilla(inscripcion.Cursante.Id, DateTime.Today.AddDays(-4), planilla => planilla.MarcarPresente(inscripcion.Cursante.Id));
		SembrarPlanilla(inscripcion.Cursante.Id, DateTime.Today.AddDays(-3), planilla => planilla.MarcarTardanza(inscripcion.Cursante.Id, TimeSpan.FromMinutes(15)));
		SembrarPlanilla(inscripcion.Cursante.Id, DateTime.Today.AddDays(-2), planilla => planilla.MarcarAusencia(inscripcion.Cursante.Id));
		SembrarPlanilla(inscripcion.Cursante.Id, DateTime.Today.AddDays(-1), planilla => planilla.MarcarInasistencia(inscripcion.Cursante.Id));

		await _servicio.GenerarReporteAsistenciasAsync(
			new ReporteAsistenciasRequest(CICLO, DateTime.Today.AddDays(-5), DateTime.Today, null));

		var fila = _host.Generador.Reporte.ShouldNotBeNull().Filas.Single().ToList();

		// Computadas, presentes, ausencias, inasistencias, tardanzas, asistencia.
		fila[4].ShouldBe("4");
		fila[5].ShouldBe("1");
		fila[6].ShouldBe("1");
		fila[7].ShouldBe("1");
		fila[8].ShouldBe("1");
		fila[9].ShouldContain("50");
	}

	[Fact]
	public async Task GenerarReporteAsistencias_sin_planillas_informa_al_alumno_sin_registros()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		// A diferencia del certificado de asistencia, el reporte SÍ se emite sin planillas: su
		// trabajo es mostrar el estado, y "cero jornadas computadas" es el estado.
		await _servicio.GenerarReporteAsistenciasAsync(
			new ReporteAsistenciasRequest(CICLO, DateTime.Today.AddDays(-5), DateTime.Today, null));

		var fila = _host.Generador.Reporte.ShouldNotBeNull().Filas.Single().ToList();
		fila[4].ShouldBe("0");
		fila[9].ShouldBe("—");
	}

	[Fact]
	public async Task GenerarReporteAsistencias_declara_el_periodo_en_el_alcance()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		var desde = new DateTime(DateTime.Today.Year, 3, 1);
		await _servicio.GenerarReporteAsistenciasAsync(new ReporteAsistenciasRequest(CICLO, desde, DateTime.Today, null));

		_host.Generador.Reporte.ShouldNotBeNull().Alcance.ShouldContain(desde.ToString("dd/MM/yyyy"));
	}

	[Fact]
	public async Task GenerarReporteAsistencias_con_el_periodo_invertido_falla()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.GenerarReporteAsistenciasAsync(
				new ReporteAsistenciasRequest(CICLO, DateTime.Today, DateTime.Today.AddDays(-5), null)));

		_host.Generador.Invocaciones.ShouldBe(0);
	}

	private void SembrarPlanilla(Guid cursanteID, DateTime fecha, Action<PlanillaAsistencia> marcar)
	{
		var planilla = PlanillaAsistencia.Abrir(_division.Id, fecha, Guid.NewGuid(), new[] { cursanteID });
		marcar(planilla);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);
	}
	#endregion

	#region Calificaciones
	[Fact]
	public async Task GenerarReporteCalificaciones_emite_una_fila_por_alumno_y_materia()
	{
		SembrarEstructura("Historia", "Matemática");
		var inscripcion = SembrarInscripcion("AL0001");
		SembrarInscripcion("AL0002");

		inscripcion.Cursante.RegistrarCalificacion(_materias[1].Id, DateTime.Today, Instancia.Parcial, 8, null);

		await _servicio.GenerarReporteCalificacionesAsync(TodaLaInstitucion());

		var reporte = _host.Generador.Reporte.ShouldNotBeNull();
		// Dos alumnos por dos materias.
		reporte.Filas.Count.ShouldBe(4);

		var calificada = reporte.Filas.Single(fila => fila.Contains("Matemática") && fila.Contains("Aprobada"));
		calificada.ShouldContain("8");
	}
	#endregion

	#region Materias pendientes
	[Fact]
	public async Task GenerarReporteMateriasPendientes_omite_las_materias_aprobadas()
	{
		SembrarEstructura("Historia", "Matemática");
		var inscripcion = SembrarInscripcion("AL0001");

		inscripcion.Cursante.RegistrarCalificacion(_materias[1].Id, DateTime.Today, Instancia.Parcial, 8, null);

		await _servicio.GenerarReporteMateriasPendientesAsync(TodaLaInstitucion());

		var reporte = _host.Generador.Reporte.ShouldNotBeNull();
		reporte.Filas.Count.ShouldBe(1);
		reporte.Filas.Single().ShouldContain("Historia");
	}
	#endregion

	#region Trayectorias
	[Fact]
	public async Task GenerarReporteTrayectorias_cuenta_aprobadas_pendientes_y_avance()
	{
		SembrarEstructura("Historia", "Lengua", "Matemática", "Química");
		var inscripcion = SembrarInscripcion("AL0001", recursante: true);

		inscripcion.Cursante.RegistrarCalificacion(_materias[0].Id, DateTime.Today, Instancia.Parcial, 7, null);
		inscripcion.Cursante.RegistrarCalificacion(_materias[1].Id, DateTime.Today, Instancia.Parcial, 10, null);

		await _servicio.GenerarReporteTrayectoriasAsync(TodaLaInstitucion());

		var fila = _host.Generador.Reporte.ShouldNotBeNull().Filas.Single().ToList();

		// Recursa, materias, aprobadas, pendientes, avance.
		fila[4].ShouldBe("Sí");
		fila[5].ShouldBe("4");
		fila[6].ShouldBe("2");
		fila[7].ShouldBe("2");
		fila[8].ShouldContain("50");
	}

	[Fact]
	public async Task GenerarReporteTrayectorias_sin_curricula_vigente_no_divide_por_cero()
	{
		SembrarEstructura();
		SembrarInscripcion("AL0001");

		await _servicio.GenerarReporteTrayectoriasAsync(TodaLaInstitucion());

		var fila = _host.Generador.Reporte.ShouldNotBeNull().Filas.Single().ToList();
		fila[5].ShouldBe("0");
		fila[8].ShouldBe("—");
	}
	#endregion
}
