using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class CursanteRepositorioTests : BaseIntegracion
{
	public CursanteRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task ExisteInscripcionAsync_es_true_cuando_hay_inscripcion_del_alumno_en_el_ciclo()
	{
		var alumnoID = await SembrarAlumnoAsync();
		var divisionID = await SembrarDivisionAsync(await SembrarCursoAsync());
		var ciclo = CicloLectivo.Crear("2024");
		var cursante = new CursanteBuilder()
			.ConAlumno(alumnoID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(cursante);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Cursantes.ExisteInscripcionAsync(alumnoID, ciclo)).ShouldBeTrue();
		var otroCiclo = CicloLectivo.Crear("2025");
		(await uow.Cursantes.ExisteInscripcionAsync(alumnoID, otroCiclo)).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task ContarCursantesDeDivisionAsync_devuelve_cantidad_de_cursantes_activos()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var ciclo = CicloLectivo.Crear("2024");
		var alumno1ID = await SembrarAlumnoAsync();
		var alumno2ID = await SembrarAlumnoAsync();

		var cursante1 = new CursanteBuilder()
			.ConAlumno(alumno1ID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();
		var cursante2 = new CursanteBuilder()
			.ConAlumno(alumno2ID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(cursante1, cursante2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cuenta = await uow.Cursantes.ContarCursantesDeDivisionAsync(divisionID, ciclo);
		cuenta.ShouldBe(2);
	}

	[RequiereSqlServerFact]
	public async Task BuscarInscripcionActivaAsync_devuelve_la_inscripcion_activa_del_alumno()
	{
		var alumnoID = await SembrarAlumnoAsync();
		var divisionID = await SembrarDivisionAsync(await SembrarCursoAsync());
		var ciclo = CicloLectivo.Crear("2024");
		var cursante = new CursanteBuilder()
			.ConAlumno(alumnoID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(cursante);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		encontrada.ShouldNotBeNull();
		encontrada.Id.ShouldBe(cursante.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarInscripcionActivaAsync_no_devuelve_si_no_existe()
	{
		var alumnoID = await SembrarAlumnoAsync();
		var divisionID = await SembrarDivisionAsync(await SembrarCursoAsync());
		var ciclo = CicloLectivo.Crear("2024");

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		encontrada.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDivisionAsync_devuelve_todos_los_cursantes_de_la_division_en_el_ciclo()
	{
		var cursoID = await SembrarCursoAsync();
		var division1ID = await SembrarDivisionAsync(cursoID, "A");
		var division2ID = await SembrarDivisionAsync(cursoID, "B");
		var ciclo = CicloLectivo.Crear("2024");
		var alumno1ID = await SembrarAlumnoAsync();
		var alumno2ID = await SembrarAlumnoAsync();
		var alumno3ID = await SembrarAlumnoAsync();

		var cursante1 = new CursanteBuilder().ConAlumno(alumno1ID).ConDivision(division1ID).ConCicloLectivo(ciclo).Build();
		var cursante2 = new CursanteBuilder().ConAlumno(alumno2ID).ConDivision(division1ID).ConCicloLectivo(ciclo).Build();
		var cursante3 = new CursanteBuilder().ConAlumno(alumno3ID).ConDivision(division2ID).ConCicloLectivo(ciclo).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(cursante1, cursante2, cursante3);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cursantesDiv1 = await uow.Cursantes.BuscarPorDivisionAsync(division1ID, ciclo);
		cursantesDiv1.Count.ShouldBe(2);
		cursantesDiv1.ShouldAllBe(x => x.DivisionID == division1ID && x.CicloLectivo.Periodo == ciclo.Periodo);

		var cursantesDiv2 = await uow.Cursantes.BuscarPorDivisionAsync(division2ID, ciclo);
		cursantesDiv2.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task BuscarInscriptosEnFechaAsync_devuelve_ids_de_cursantes_dentro_del_rango_de_fechas()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var hoy = DateTime.Today;
		var alumno1ID = await SembrarAlumnoAsync();
		var alumno2ID = await SembrarAlumnoAsync();

		var cursante1 = new CursanteBuilder()
			.ConAlumno(alumno1ID)
			.ConDivision(divisionID)
			.ConFechaInicio(hoy.AddDays(-10))
			.Build();
		var cursante2 = new CursanteBuilder()
			.ConAlumno(alumno2ID)
			.ConDivision(divisionID)
			.ConFechaInicio(hoy.AddDays(5))
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(cursante1, cursante2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var inscritos = await uow.Cursantes.BuscarInscriptosEnFechaAsync(divisionID, hoy);
		// Solo cursante1 está vigente en esa fecha (FechaInicio <= hoy)
		inscritos.Count.ShouldBe(1);
		inscritos.ShouldContain(cursante1.Id);
		inscritos.ShouldNotContain(cursante2.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_carga_las_calificaciones_del_cursante()
	{
		var cursoID = await SembrarCursoAsync();
		var curriculaID = await SembrarCurriculaAsync(cursoID);
		var divisionID = await SembrarDivisionAsync(cursoID);
		var materiaID = await SembrarMateriaAsync(curriculaID);
		var alumnoID = await SembrarAlumnoAsync();
		var ciclo = CicloLectivo.Crear("2024");
		var cursante = new CursanteBuilder()
			.ConAlumno(alumnoID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.Build();

		// Registrar una calificación real
		cursante.RegistrarCalificacion(materiaID, DateTime.Today, Instancia.Parcial, 8.5, null);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(cursante);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cargado = await uow.Cursantes.BuscarPorIDAsync(cursante.Id);
		cargado.ShouldNotBeNull();
		cargado.Calificaciones.Count.ShouldBe(1);
		cargado.Calificaciones.First().MateriaID.ShouldBe(materiaID);
		cargado.Calificaciones.First().Nota.ShouldBe(8.5);
	}
}
