using Domain.Asistencias;
using Domain.Cursantes;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class PlanillaAsistenciaRepositorioTests : BaseIntegracion
{
	public PlanillaAsistenciaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	/// <summary>
	/// Persiste un cursante real en la división y devuelve su <c>Id</c>, listo para ponerlo en una
	/// planilla.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Existe porque seis de las ocho pruebas de esta clase inventaban los cursantes con
	/// <c>Guid.NewGuid()</c>, y ninguna podía insertar.</strong> <c>FK_REGISTRO_ASISTENCIA_CURSANTE</c>
	/// apunta a <c>cursante</c>, así que un identificador suelto hace fallar el <c>INSERT</c> de la
	/// planilla con una violación de clave ajena. El defecto estuvo latente desde que se escribieron
	/// porque la suite nunca se había ejecutado contra SQL Server: sin Docker ni
	/// <c>EDUSIS_TEST_SQLSERVER</c> estas pruebas se <em>omiten</em>, y una prueba omitida no
	/// distingue entre correcta y roja.
	/// </para>
	/// <para>
	/// Es exactamente la ceguera que CORE-23 e INFRA-19 describen, pero del otro lado: no es que el
	/// fake no detecte lo que falla en EF, es que la prueba que sí iría a EF no se corría.
	/// </para>
	/// </remarks>
	private async Task<Guid> SembrarCursanteAsync(Guid divisionID, string periodo = "2024")
	{
		var cursante = new CursanteBuilder()
			.ConAlumno(await SembrarAlumnoAsync())
			.ConDivision(divisionID)
			.ConCicloLectivo(CicloLectivo.Crear(periodo))
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(cursante);
		await contexto.SaveChangesAsync();
		return cursante.Id;
	}

	[RequiereSqlServerFact]
	public async Task ExistePlanillaAsync_es_true_cuando_existe_planilla_de_esa_division_y_fecha()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var hoy = DateTime.Today;
		var cursante1ID = await SembrarCursanteAsync(divisionID);
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.PlanillasAsistencia.ExistePlanillaAsync(divisionID, hoy)).ShouldBeTrue();
		(await uow.PlanillasAsistencia.ExistePlanillaAsync(divisionID, hoy.AddDays(1))).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDivisionYFechaAsync_devuelve_la_planilla_de_esa_division_y_fecha()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var hoy = DateTime.Today;
		var cursante1ID = await SembrarCursanteAsync(divisionID);
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.PlanillasAsistencia.BuscarPorDivisionYFechaAsync(divisionID, hoy);
		encontrada.ShouldNotBeNull();
		encontrada.Id.ShouldBe(planilla.Id);

		var noEncontrada = await uow.PlanillasAsistencia.BuscarPorDivisionYFechaAsync(divisionID, hoy.AddDays(1));
		noEncontrada.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task ContarFaltasAsync_cuenta_faltas_de_un_cursante()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var cursante1ID = await SembrarCursanteAsync(divisionID);
		var cursante2ID = await SembrarCursanteAsync(divisionID);
		var hoy = DateTime.Today;

		var planilla1 = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID, cursante2ID)
			.Build();
		planilla1.MarcarAusencia(cursante1ID);

		var planilla2 = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy.AddDays(1))
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID, cursante2ID)
			.Build();
		planilla2.MarcarAusencia(cursante1ID);
		planilla2.MarcarAusencia(cursante2ID);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(planilla1, planilla2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var faltas1 = await uow.PlanillasAsistencia.ContarFaltasAsync(cursante1ID, TipoAsistencia.Ausencia);
		faltas1.ShouldBe(2);

		var faltas2 = await uow.PlanillasAsistencia.ContarFaltasAsync(cursante2ID, TipoAsistencia.Ausencia);
		faltas2.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_carga_los_registros_de_asistencia()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var cursante1ID = await SembrarCursanteAsync(divisionID);
		var hoy = DateTime.Today;
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cargada = await uow.PlanillasAsistencia.BuscarPorIDAsync(planilla.Id);
		cargada.ShouldNotBeNull();
		cargada.Registros.ShouldNotBeNull();
		cargada.Registros.Count.ShouldBeGreaterThan(0);
	}

	#region BuscarPorCursantesAsync
	/// <summary>
	/// La consulta que alimenta el borrado de un alumno, contra SQL Server real.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Es la pieza del borrado en cascada que no se puede verificar con fakes.</strong> El
	/// <c>Where(x =&gt; x.Registros.Any(r =&gt; ids.Contains(r.CursanteID)))</c> es un <c>Any</c> sobre
	/// una colección de navegación combinado con un <c>Contains</c> sobre una lista en memoria: en el
	/// fake eso es LINQ to Objects y pasa siempre. Lo que se prueba acá es que EF lo <em>traduzca</em>
	/// a un <c>EXISTS</c> con <c>IN (…)</c> en vez de romper o evaluar del lado del cliente.
	/// </para>
	/// <para>
	/// Se siembran cursantes reales y no <c>Guid.NewGuid()</c> sueltos porque
	/// <c>FK_REGISTRO_ASISTENCIA_CURSANTE</c> apunta a <c>cursante</c>, y porque el escenario que
	/// importa es el real: el alumno que se va tiene inscripciones de verdad.
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarPorCursantesAsync_trae_solo_las_planillas_con_registro_de_esos_cursantes()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var ciclo = CicloLectivo.Crear("2024");

		var buscado = new CursanteBuilder()
			.ConAlumno(await SembrarAlumnoAsync())
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();
		var ajeno = new CursanteBuilder()
			.ConAlumno(await SembrarAlumnoAsync())
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();

		var conElBuscado = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(DateTime.Today.AddDays(-1))
			.ConPreceptor(preceptorID)
			.ConCursantes(buscado.Id, ajeno.Id)
			.Build();
		var sinElBuscado = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(DateTime.Today)
			.ConPreceptor(preceptorID)
			.ConCursantes(ajeno.Id)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(buscado, ajeno);
			await contexto.SaveChangesAsync();

			contexto.AddRange(conElBuscado, sinElBuscado);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontradas = await uow.PlanillasAsistencia.BuscarPorCursantesAsync(new[] { buscado.Id });

		encontradas.ShouldHaveSingleItem().Id.ShouldBe(conElBuscado.Id);

		// Los registros vienen cargados: el borrado los recorre para quitar los del alumno.
		encontradas.Single().Registros.Count.ShouldBe(2);
	}

	/// <remarks>
	/// Un alumno sin una sola inscripción es el caso corriente, y ahí la lista llega vacía. El
	/// repositorio corta antes de ir a la base: un <c>IN ()</c> vacío no tiene traducción útil.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarPorCursantesAsync_sin_cursantes_devuelve_vacio()
	{
		using var uow = CrearUnidadDeTrabajo();

		var encontradas = await uow.PlanillasAsistencia.BuscarPorCursantesAsync(Array.Empty<Guid>());

		encontradas.ShouldBeEmpty();
	}
	#endregion

	#region BuscarPorDivisionAsync
	/// <remarks>
	/// La consume el borrado de la división, y por eso <strong>no filtra por fecha</strong>:
	/// <c>FK_PLANILLA_ASISTENCIA_DIVISION</c> es <c>Restrict</c>, así que una planilla olvidada de
	/// cualquier año alcanza para que el DELETE falle en SQL Server. Esta prueba siembra una planilla
	/// vieja a propósito: con un filtro de fecha o de ciclo, no aparecería.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarPorDivisionAsync_trae_las_planillas_de_cualquier_fecha_y_no_las_de_otra_division()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var otraDivisionID = await SembrarDivisionAsync(cursoID, "B");
		var preceptorID = await SembrarDocenteAsync();

		var unCursante = await SembrarCursanteAsync(divisionID);
		var otroCursante = await SembrarCursanteAsync(divisionID);
		var cursanteAjeno = await SembrarCursanteAsync(otraDivisionID);

		var vieja = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(DateTime.Today.AddYears(-3))
			.ConPreceptor(preceptorID)
			.ConCursantes(unCursante)
			.Build();
		var reciente = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(DateTime.Today)
			.ConPreceptor(preceptorID)
			.ConCursantes(unCursante, otroCursante)
			.Build();
		var ajena = new PlanillaAsistenciaBuilder()
			.ConDivision(otraDivisionID)
			.ConFecha(DateTime.Today)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursanteAjeno)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(vieja, reciente, ajena);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontradas = await uow.PlanillasAsistencia.BuscarPorDivisionAsync(divisionID);

		encontradas.Count.ShouldBe(2);
		encontradas.Select(x => x.Id).ShouldBe(new[] { vieja.Id, reciente.Id }, ignoreOrder: true);

		// Los registros vienen cargados por el Include de Consulta(): el impacto los cuenta.
		encontradas.Sum(x => x.Registros.Count).ShouldBe(3);
	}

	/// <remarks>
	/// Que el borrado emita los DELETE de verdad es lo que el fake no puede acreditar: en memoria
	/// quitar de una lista siempre "funciona". Acá se ejercita contra la base, con los
	/// <c>registro_asistencia</c> cayendo por <c>FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA</c>, que
	/// es <c>Cascade</c>.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task EliminarRango_de_planillas_se_lleva_sus_registros_por_cascada()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();

		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(DateTime.Today)
			.ConPreceptor(preceptorID)
			.ConCursantes(await SembrarCursanteAsync(divisionID), await SembrarCursanteAsync(divisionID))
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using (var uow = CrearUnidadDeTrabajo())
		{
			var planillas = await uow.PlanillasAsistencia.BuscarPorDivisionAsync(divisionID);
			uow.PlanillasAsistencia.EliminarRango(planillas);
			await uow.GuardarCambiosAsync();
		}

		using var verificacion = CrearUnidadDeTrabajo();
		(await verificacion.PlanillasAsistencia.BuscarPorDivisionAsync(divisionID)).ShouldBeEmpty();
	}
	#endregion
}
