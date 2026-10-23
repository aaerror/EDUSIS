using Core.ServicioDocumentos;
using Core.ServicioDocumentos.DTOs.Requests;
using Core.Shared.Documentos;
using Core.Shared.Seguridad;
using Core.UnitTests.Infraestructura;
using Domain.Alumnos;
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

namespace Core.UnitTests.ServicioDocumentos;

/// <summary>
/// <see cref="IServicioDocumento"/>: los seis documentos de alumno. Las pruebas afirman sobre el
/// documento que recibió el puerto <see cref="IGeneradorDocumentos"/> —doblado con
/// <see cref="GeneradorDocumentosFake"/>—, de modo que se verifica QUÉ dice cada documento sin
/// renderizar un PDF ni tocar el disco.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioDocumentoTests
{
	private const string CICLO = "2026";

	private readonly HostDeServicios _host = new();
	private readonly IServicioDocumento _servicio;

	public ServicioDocumentoTests()
	{
		_servicio = _host.Resolver<IServicioDocumento>();
	}

	#region Siembra
	/// <summary>
	/// Alumno inscripto en una división de un curso, con currícula vigente y las materias
	/// indicadas. Devuelve lo que las pruebas necesitan nombrar.
	/// </summary>
	private (Alumno Alumno, Cursante Cursante, Division Division, Curso Curso, IReadOnlyList<Materia> Materias) Sembrar(
		params string[] materias)
	{
		var curso = new CursoBuilder().ConGrado("Tercero").ConNivelEducativo("Secundaria").Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);

		var division = new DivisionBuilder().ConCurso(curso.Id).ConDescripcion("B").Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);

		var alumno = new AlumnoBuilder().ConLegajo("AL1234").Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);

		var cursante = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(division.Id)
			.ConCicloLectivo(CicloLectivo.Crear(CICLO))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		var curricula = new CurriculaBuilder().ConCurso(curso.Id).ConFechaFin(null).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);

		var sembradas = materias
			.Select(descripcion =>
				{
					var materia = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion(descripcion).Build();
					_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);
					return materia;
				})
			.ToList();

		return (alumno, cursante, division, curso, sembradas);
	}

	private static EmitirDocumentoAlumnoRequest Pedido(Guid alumnoID, string? finalidad = null) =>
		new(alumnoID, CICLO, finalidad);
	#endregion

	#region Certificado de alumno regular
	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_compone_la_identificacion_y_la_trayectoria_del_alumno()
	{
		var sembrado = Sembrar();

		await _servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.CertificadoAlumnoRegular.ShouldNotBeNull();
		documento.CicloLectivo.ShouldBe(CICLO);
		documento.Alumno.NombreCompleto.ShouldBe(sembrado.Alumno.DatosPersonales.NombreCompleto());
		documento.Alumno.Documento.ShouldBe(sembrado.Alumno.DatosPersonales.Documento);
		documento.Alumno.Legajo.ShouldBe("AL1234");
		documento.Alumno.NivelEducativo.ShouldBe("Secundaria");
		documento.Alumno.Curso.ShouldBe("3° Año");
		documento.Alumno.Division.ShouldBe("B");
		documento.FechaIngreso.ShouldBe(sembrado.Alumno.Periodo.FechaInicio);
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_declara_el_lugar_de_expedicion_de_la_institucion()
	{
		var sembrado = Sembrar();

		await _servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(sembrado.Alumno.Id, "Trámite de beca"));

		var emision = _host.Generador.CertificadoAlumnoRegular.ShouldNotBeNull().Emision;
		emision.Localidad.ShouldBe(new DatosInstitucionalesDePrueba().Localidad);
		emision.Fecha.ShouldBe(DateTime.Today);
		emision.Finalidad.ShouldBe("Trámite de beca");
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_sin_finalidad_no_declara_motivo()
	{
		var sembrado = Sembrar();

		// Cadena en blanco y nulo tienen que dar lo mismo: el documento no declara finalidad.
		await _servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(sembrado.Alumno.Id, "   "));

		_host.Generador.CertificadoAlumnoRegular.ShouldNotBeNull().Emision.Finalidad.ShouldBeNull();
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_sin_permiso_no_llega_al_generador()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.SinPermisos());
		var servicio = host.Resolver<IServicioDocumento>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(Guid.NewGuid())));

		host.Generador.Invocaciones.ShouldBe(0);
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_con_permiso_de_reportes_no_alcanza()
	{
		// Los dos permisos son distintos a propósito: quien emite reportes institucionales no
		// necesariamente está habilitado a emitir documentación de un alumno.
		using var host = new HostDeServicios(sesion: SesionDePrueba.Con(Permiso.GenerarReportes));
		var servicio = host.Resolver<IServicioDocumento>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(Guid.NewGuid())));
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_de_un_alumno_sin_inscripcion_en_el_ciclo_falla()
	{
		var alumno = new AlumnoBuilder().Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);

		var ex = await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(alumno.Id)));

		ex.Message.ShouldContain(CICLO);
		_host.Generador.Invocaciones.ShouldBe(0);
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_propaga_la_excepcion_del_generador_sin_transformarla()
	{
		var sembrado = Sembrar();
		_host.Generador.Falla = new InvalidOperationException("Falla del generador de PDF");

		var ex = await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.GenerarCertificadoAlumnoRegularAsync(Pedido(sembrado.Alumno.Id)));

		ex.Message.ShouldBe("Falla del generador de PDF");
	}
	#endregion

	#region Constancia de escolaridad
	[Fact]
	public async Task GenerarConstanciaEscolaridad_informa_el_ciclo_y_la_fecha_de_escolarizacion()
	{
		var sembrado = Sembrar();

		await _servicio.GenerarConstanciaEscolaridadAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.ConstanciaEscolaridad.ShouldNotBeNull();
		documento.CicloLectivo.ShouldBe(CICLO);
		documento.FechaIngreso.ShouldBe(sembrado.Alumno.Periodo.FechaInicio);
		documento.Alumno.Legajo.ShouldBe("AL1234");
	}
	#endregion

	#region Certificado de asistencia
	[Fact]
	public async Task GenerarCertificadoAsistencia_acredita_el_periodo_pedido()
	{
		var sembrado = Sembrar();
		var ayer = DateTime.Today.AddDays(-1);
		SembrarPlanillaCon(sembrado.Division.Id, sembrado.Cursante.Id, ayer);

		await _servicio.GenerarCertificadoAsistenciaAsync(
			new EmitirCertificadoAsistenciaRequest(sembrado.Alumno.Id, CICLO, ayer, DateTime.Today, null));

		var documento = _host.Generador.CertificadoAsistencia.ShouldNotBeNull();
		documento.Periodo.Desde.ShouldBe(ayer.Date);
		documento.Periodo.Hasta.ShouldBe(DateTime.Today);
	}

	[Fact]
	public async Task GenerarCertificadoAsistencia_sin_planillas_en_el_periodo_no_certifica()
	{
		var sembrado = Sembrar();

		// Sin planillas no hay respaldo de la concurrencia: acreditarla sería afirmar algo que el
		// sistema no registró.
		var ex = await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.GenerarCertificadoAsistenciaAsync(
				new EmitirCertificadoAsistenciaRequest(sembrado.Alumno.Id, CICLO, DateTime.Today.AddDays(-10), DateTime.Today, null)));

		ex.Message.ShouldContain("planillas de asistencia");
		_host.Generador.Invocaciones.ShouldBe(0);
	}

	[Fact]
	public async Task GenerarCertificadoAsistencia_con_el_periodo_invertido_falla()
	{
		var sembrado = Sembrar();

		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.GenerarCertificadoAsistenciaAsync(
				new EmitirCertificadoAsistenciaRequest(sembrado.Alumno.Id, CICLO, DateTime.Today, DateTime.Today.AddDays(-5), null)));

		_host.Generador.Invocaciones.ShouldBe(0);
	}

	private void SembrarPlanillaCon(Guid divisionID, Guid cursanteID, DateTime fecha)
	{
		var planilla = Domain.Asistencias.PlanillaAsistencia.Abrir(divisionID, fecha, Guid.NewGuid(), new[] { cursanteID });
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);
	}
	#endregion

	#region Constancias de materias
	[Fact]
	public async Task GenerarConstanciaMateriasAprobadas_informa_solo_las_materias_aprobadas()
	{
		var sembrado = Sembrar("Matemática", "Lengua", "Historia");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 8);
		Calificar(sembrado.Cursante, sembrado.Materias[1], nota: 4);
		// Historia queda sin calificar.

		await _servicio.GenerarConstanciaMateriasAprobadasAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.ConstanciaMaterias.ShouldNotBeNull();
		documento.Tipo.ShouldBe(TipoConstanciaMaterias.Aprobadas);
		documento.Materias.Select(x => x.Materia).ShouldBe(new[] { "Matemática" });
		documento.Materias.Single().Condicion.ShouldBe("Aprobada (8)");
	}

	[Fact]
	public async Task GenerarConstanciaMateriasPendientes_informa_las_desaprobadas_y_las_sin_calificar()
	{
		var sembrado = Sembrar("Matemática", "Lengua", "Historia");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 8);
		Calificar(sembrado.Cursante, sembrado.Materias[1], nota: 4);

		await _servicio.GenerarConstanciaMateriasPendientesAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.ConstanciaMaterias.ShouldNotBeNull();
		documento.Tipo.ShouldBe(TipoConstanciaMaterias.Pendientes);
		// Ordenadas por descripción: Historia antes que Lengua.
		documento.Materias.Select(x => x.Materia).ShouldBe(new[] { "Historia", "Lengua" });
		documento.Materias.Single(x => x.Materia == "Historia").Condicion.ShouldBe("Sin calificar");
		documento.Materias.Single(x => x.Materia == "Lengua").Condicion.ShouldBe("Desaprobada (4)");
	}

	[Fact]
	public async Task GenerarConstanciaMateriasAprobadas_cuenta_aprobar_en_el_recuperatorio()
	{
		var sembrado = Sembrar("Matemática");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 3);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, instancia: Instancia.Recuperatorio);

		await _servicio.GenerarConstanciaMateriasAprobadasAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.ConstanciaMaterias.ShouldNotBeNull();
		documento.Materias.Select(x => x.Materia).ShouldBe(new[] { "Matemática" });
		// La condición informa la MEJOR nota, que es la que explica la aprobación.
		documento.Materias.Single().Condicion.ShouldBe("Aprobada (7)");
	}

	[Fact]
	public async Task GenerarConstanciaMateriasAprobadas_sin_curricula_vigente_entrega_la_tabla_vacia()
	{
		// Sin currícula vigente no hay espacios curriculares que informar, pero el documento se
		// emite igual: la tabla vacía es información, no un error de operación.
		var sembrado = Sembrar();

		await _servicio.GenerarConstanciaMateriasAprobadasAsync(Pedido(sembrado.Alumno.Id));

		_host.Generador.ConstanciaMaterias.ShouldNotBeNull().Materias.ShouldBeEmpty();
	}
	#endregion

	#region Boletín
	[Fact]
	public async Task GenerarBoletin_informa_una_fila_por_materia_con_la_nota_de_cada_instancia()
	{
		var sembrado = Sembrar("Matemática", "Lengua");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 3);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, instancia: Instancia.Recuperatorio);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.Boletin.ShouldNotBeNull();
		documento.Filas.Count.ShouldBe(2);

		var matematica = documento.Filas.Single(x => x.Materia == "Matemática");
		matematica.Parcial.ShouldBe("3");
		matematica.Recuperatorio.ShouldBe("7");
		matematica.Condicion.ShouldBe("Aprobada (7)");

		var lengua = documento.Filas.Single(x => x.Materia == "Lengua");
		lengua.Parcial.ShouldBe("—");
		lengua.Recuperatorio.ShouldBe("—");
		lengua.Condicion.ShouldBe("Sin calificar");
	}

	[Fact]
	public async Task GenerarBoletin_informa_la_ultima_calificacion_por_fecha_de_cada_instancia()
	{
		var sembrado = Sembrar("Matemática");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 2, fecha: DateTime.Today.AddDays(-20));
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 9, fecha: DateTime.Today.AddDays(-2));

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		_host.Generador.Boletin.ShouldNotBeNull().Filas.Single().Parcial.ShouldBe("9");
	}

	[Fact]
	public async Task GenerarBoletin_distingue_no_haber_rendido_de_no_tener_nota()
	{
		var sembrado = Sembrar("Matemática");
		sembrado.Cursante.RegistrarInasistenciaAExamen(sembrado.Materias[0].Id, DateTime.Today, Instancia.Parcial, null);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var fila = _host.Generador.Boletin.ShouldNotBeNull().Filas.Single();
		fila.Parcial.ShouldBe("No rindió");
		fila.Recuperatorio.ShouldBe("—");
		fila.Condicion.ShouldBe("No rindió");
	}

	private static void Calificar(Cursante cursante, Materia materia, double nota, Instancia instancia = Instancia.Parcial, DateTime? fecha = null) =>
		cursante.RegistrarCalificacion(materia.Id, fecha ?? DateTime.Today, instancia, nota, null);
	#endregion
}
