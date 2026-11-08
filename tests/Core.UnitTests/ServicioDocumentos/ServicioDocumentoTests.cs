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

	/// <summary>
	/// Inscripción <strong>cerrada</strong> del mismo alumno y del mismo ciclo lectivo, en una
	/// división propia: es el registro que deja un traslado de división. Se siembra además de la
	/// activa de <see cref="Sembrar"/>, no en su lugar.
	/// </summary>
	private Cursante SembrarInscripcionCerrada(Guid alumnoID, Guid cursoID, string division)
	{
		var divisionAnterior = new DivisionBuilder().ConCurso(cursoID).ConDescripcion(division).Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(divisionAnterior);

		var cerrada = new CursanteBuilder()
			.ConAlumno(alumnoID)
			.ConDivision(divisionAnterior.Id)
			.ConCicloLectivo(CicloLectivo.Crear(CICLO))
			.ConFechaInicio(DateTime.Today.AddDays(-60))
			.DadoDeBajaEl(DateTime.Today.AddDays(-30))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cerrada);

		return cerrada;
	}
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
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, esRecuperatorio: true);

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
	/// <remarks>
	/// LA COLUMNA DE UN CUATRIMESTRE INFORMA SU NOTA VIGENTE, NO SUS INTENTOS. El recuperatorio
	/// reemplaza a la nota del período, así que con un 3 recuperado con 7 la celda dice 7 y el 3 no
	/// aparece en el boletín. Antes había dos columnas, "Parcial" y "Recuperatorio", y mostraban
	/// las dos notas; con cuatro recuperatorios posibles en un ciclo esas dos columnas no alcanzan,
	/// y el detalle intento por intento vive en la pantalla de calificaciones.
	/// </remarks>
	[Fact]
	public async Task GenerarBoletin_informa_una_fila_por_materia_con_la_nota_vigente_de_cada_periodo()
	{
		var sembrado = Sembrar("Matemática", "Lengua");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 3);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, esRecuperatorio: true);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.Boletin.ShouldNotBeNull();
		documento.Filas.Count.ShouldBe(2);

		var matematica = documento.Filas.Single(x => x.Materia == "Matemática");
		matematica.PrimerCuatrimestre.ShouldBe("7");
		matematica.SegundoCuatrimestre.ShouldBe("—");
		matematica.Final.ShouldBe("—");
		matematica.Recuperacion.ShouldBe("—");
		matematica.Condicion.ShouldBe("Aprobada (7)");

		var lengua = documento.Filas.Single(x => x.Materia == "Lengua");
		lengua.PrimerCuatrimestre.ShouldBe("—");
		lengua.SegundoCuatrimestre.ShouldBe("—");
		lengua.Condicion.ShouldBe("Sin calificar");
	}

	/// <remarks>
	/// La nota final la carga el docente y PISA al promedio de los cuatrimestres, incluso cuando es
	/// distinta: es su decisión, no una cuenta del sistema. Con 4 y 8 el promedio sería 6, y acá el
	/// docente cerró en 7.
	/// </remarks>
	[Fact]
	public async Task GenerarBoletin_informa_la_nota_final_y_cierra_con_ella()
	{
		var sembrado = Sembrar("Matemática");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 4, periodo: Periodo.PrimerCuatrimestre);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 8, periodo: Periodo.SegundoCuatrimestre);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, periodo: Periodo.Final);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var fila = _host.Generador.Boletin.ShouldNotBeNull().Filas.Single();
		fila.PrimerCuatrimestre.ShouldBe("4");
		fila.SegundoCuatrimestre.ShouldBe("8");
		fila.Final.ShouldBe("7");
		fila.Condicion.ShouldBe("Aprobada (7)");
	}

	/// <remarks>
	/// Las dos recuperaciones comparten columna y vale la segunda: a ella se llega sólo después de
	/// no aprobar la primera, así que es la última palabra sobre la materia.
	/// </remarks>
	[Fact]
	public async Task GenerarBoletin_cierra_con_la_recuperacion_mas_avanzada()
	{
		var sembrado = Sembrar("Matemática");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 3, periodo: Periodo.PrimerCuatrimestre);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 4, periodo: Periodo.Final);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 5, periodo: Periodo.PrimeraRecuperacion);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 6, periodo: Periodo.SegundaRecuperacion);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var fila = _host.Generador.Boletin.ShouldNotBeNull().Filas.Single();
		fila.Recuperacion.ShouldBe("6");
		fila.Condicion.ShouldBe("Aprobada (6)");
	}

	[Fact]
	public async Task GenerarBoletin_informa_la_ultima_calificacion_por_fecha_del_periodo()
	{
		var sembrado = Sembrar("Matemática");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 2, fecha: DateTime.Today.AddDays(-20));
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 9, fecha: DateTime.Today.AddDays(-2));

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		_host.Generador.Boletin.ShouldNotBeNull().Filas.Single().PrimerCuatrimestre.ShouldBe("9");
	}

	[Fact]
	public async Task GenerarBoletin_distingue_no_haber_rendido_de_no_tener_nota()
	{
		var sembrado = Sembrar("Matemática");
		sembrado.Cursante.RegistrarInasistenciaAExamen(sembrado.Materias[0].Id, DateTime.Today, Periodo.PrimerCuatrimestre, false, null);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var fila = _host.Generador.Boletin.ShouldNotBeNull().Filas.Single();
		fila.PrimerCuatrimestre.ShouldBe("No rindió");
		fila.SegundoCuatrimestre.ShouldBe("—");
		fila.Condicion.ShouldBe("No rindió");
	}

	private static void Calificar(Cursante cursante, Materia materia, double nota, Periodo periodo = Periodo.PrimerCuatrimestre, bool esRecuperatorio = false, DateTime? fecha = null) =>
		cursante.RegistrarCalificacion(materia.Id, fecha ?? DateTime.Today, periodo, esRecuperatorio, nota, null);
	#endregion

	#region Boletín de un alumno trasladado (DOC-1)
	[Fact]
	public async Task GenerarBoletin_de_un_alumno_trasladado_informa_las_notas_de_la_inscripcion_cerrada()
	{
		// Consecuencia 1 del traslado: Calificacion es entidad hija de Cursante, así que las notas
		// cargadas antes del traslado viven en la inscripción cerrada. Sin unirlas, el boletín de un
		// alumno trasladado sale en blanco.
		var sembrado = Sembrar("Matemática");
		var cerrada = SembrarInscripcionCerrada(sembrado.Alumno.Id, sembrado.Curso.Id, "A");
		Calificar(cerrada, sembrado.Materias[0], nota: 8, fecha: DateTime.Today.AddDays(-40));

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var fila = _host.Generador.Boletin.ShouldNotBeNull().Filas.ShouldHaveSingleItem();
		fila.PrimerCuatrimestre.ShouldBe("8");
		fila.Condicion.ShouldBe("Aprobada (8)");
	}

	[Fact]
	public async Task GenerarBoletin_de_un_alumno_trasladado_encabeza_con_la_division_actual()
	{
		// El encabezado sigue saliendo de la inscripción ACTIVA: lo único que se une de la cerrada
		// son las calificaciones.
		var sembrado = Sembrar("Matemática");
		var cerrada = SembrarInscripcionCerrada(sembrado.Alumno.Id, sembrado.Curso.Id, "A");
		Calificar(cerrada, sembrado.Materias[0], nota: 8, fecha: DateTime.Today.AddDays(-40));

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		_host.Generador.Boletin.ShouldNotBeNull().Alumno.Division.ShouldBe("B");
	}

	[Fact]
	public async Task GenerarBoletin_une_las_notas_de_las_dos_inscripciones_del_ciclo()
	{
		// Una materia calificada antes del traslado y otra después: las dos tienen que figurar.
		var sembrado = Sembrar("Matemática", "Lengua");
		var cerrada = SembrarInscripcionCerrada(sembrado.Alumno.Id, sembrado.Curso.Id, "A");
		Calificar(cerrada, sembrado.Materias[0], nota: 7, fecha: DateTime.Today.AddDays(-40));
		Calificar(sembrado.Cursante, sembrado.Materias[1], nota: 9);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.Boletin.ShouldNotBeNull();
		documento.Filas.Single(x => x.Materia == "Matemática").PrimerCuatrimestre.ShouldBe("7");
		documento.Filas.Single(x => x.Materia == "Lengua").PrimerCuatrimestre.ShouldBe("9");
	}

	[Fact]
	public async Task GenerarBoletin_sin_traslado_sale_igual_que_antes()
	{
		// No-regresión: con una sola inscripción en el ciclo, unir las calificaciones de todas las
		// inscripciones da exactamente el mismo boletín que leer las del cursante activo.
		var sembrado = Sembrar("Matemática", "Lengua");
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 3);
		Calificar(sembrado.Cursante, sembrado.Materias[0], nota: 7, esRecuperatorio: true);

		await _servicio.GenerarBoletinCalificacionesAsync(Pedido(sembrado.Alumno.Id));

		var documento = _host.Generador.Boletin.ShouldNotBeNull();
		documento.Alumno.Division.ShouldBe("B");

		var matematica = documento.Filas.Single(x => x.Materia == "Matemática");
		matematica.PrimerCuatrimestre.ShouldBe("7");
		matematica.SegundoCuatrimestre.ShouldBe("—");
		matematica.Condicion.ShouldBe("Aprobada (7)");

		var lengua = documento.Filas.Single(x => x.Materia == "Lengua");
		lengua.PrimerCuatrimestre.ShouldBe("—");
		lengua.Condicion.ShouldBe("Sin calificar");
	}

	[Fact]
	public async Task GenerarConstanciaMateriasAprobadas_de_un_alumno_trasladado_cuenta_lo_aprobado_antes_del_traslado()
	{
		// Las dos constancias leen las calificaciones por la misma vía que el boletín.
		var sembrado = Sembrar("Matemática", "Lengua");
		var cerrada = SembrarInscripcionCerrada(sembrado.Alumno.Id, sembrado.Curso.Id, "A");
		Calificar(cerrada, sembrado.Materias[0], nota: 8, fecha: DateTime.Today.AddDays(-40));

		await _servicio.GenerarConstanciaMateriasAprobadasAsync(Pedido(sembrado.Alumno.Id));

		_host.Generador.ConstanciaMaterias.ShouldNotBeNull()
			.Materias.Select(x => x.Materia).ShouldBe(new[] { "Matemática" });
	}
	#endregion

	#region Ciclos lectivos
	[Fact]
	public async Task ListarCiclosLectivos_devuelve_los_periodos_habilitados()
	{
		Sembrar();

		var ciclos = await _servicio.ListarCiclosLectivosAsync();

		ciclos.ShouldContain(CICLO);
	}

	[Fact]
	public async Task ListarCiclosLectivos_sin_permiso_falla()
	{
		// El catálogo alimenta el desplegable de la pantalla de documentación, así que va detrás
		// de la misma guarda: sin permiso no se llega ni a saber qué períodos existen.
		using var host = new HostDeServicios(sesion: SesionDePrueba.SinPermisos());
		var servicio = host.Resolver<IServicioDocumento>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.ListarCiclosLectivosAsync());
	}

	[Fact]
	public async Task ListarCiclosLectivos_con_permiso_de_reportes_no_alcanza()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.Con(Permiso.GenerarReportes));
		var servicio = host.Resolver<IServicioDocumento>();

		await Should.ThrowAsync<PermisoDenegadoException>(
			() => servicio.ListarCiclosLectivosAsync());
	}
	#endregion
}
