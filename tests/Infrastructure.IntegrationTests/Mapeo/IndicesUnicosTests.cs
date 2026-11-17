using Domain.Cursantes;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// Índices únicos que respaldan en la base invariantes que <c>Core</c> ya valida con una consulta
/// previa (<c>ExisteDivisionConDescripcionAsync</c>, <c>ExisteInscripcionActivaAsync</c>). La
/// consulta no ve una alta simultánea desde otra estación; el índice sí.
/// </summary>
public sealed class IndicesUnicosTests : BaseIntegracion
{
	public IndicesUnicosTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Dos_divisiones_con_la_misma_letra_en_el_mismo_curso_violan_el_indice()
	{
		var cursoID = await SembrarCursoAsync();

		await using var contexto = Fixture.CrearContexto();
		contexto.AddRange(
			new DivisionBuilder().ConCurso(cursoID).ConDescripcion("A").Build(),
			new DivisionBuilder().ConCurso(cursoID).ConDescripcion("A").Build());

		await Should.ThrowAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
	}

	[RequiereSqlServerFact]
	public async Task La_misma_letra_en_cursos_distintos_no_viola_el_indice()
	{
		var cursoA = await SembrarCursoAsync("Primero");
		var cursoB = await SembrarCursoAsync("Segundo");

		await using var contexto = Fixture.CrearContexto();
		contexto.AddRange(
			new DivisionBuilder().ConCurso(cursoA).ConDescripcion("A").Build(),
			new DivisionBuilder().ConCurso(cursoB).ConDescripcion("A").Build());

		await Should.NotThrowAsync(() => contexto.SaveChangesAsync());
	}

	[RequiereSqlServerFact]
	public async Task Dos_inscripciones_activas_del_alumno_en_el_mismo_ciclo_violan_el_indice()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionA = await SembrarDivisionAsync(cursoID, "A");
		var divisionB = await SembrarDivisionAsync(cursoID, "B");
		var alumnoID = await SembrarAlumnoAsync();
		var ciclo = CicloLectivo.Crear("2024");

		await using var contexto = Fixture.CrearContexto();
		contexto.AddRange(
			new CursanteBuilder().ConAlumno(alumnoID).ConDivision(divisionA).ConCicloLectivo(ciclo).Build(),
			new CursanteBuilder().ConAlumno(alumnoID).ConDivision(divisionB).ConCicloLectivo(ciclo).Build());

		await Should.ThrowAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
	}

	/// <summary>
	/// El índice está filtrado por <c>fecha_fin IS NULL</c> justamente por esto: el traslado cierra
	/// la inscripción de origen y abre la de destino del mismo alumno en el mismo ciclo, en un solo
	/// <c>SaveChanges</c>. Sin filtro, o si EF mandara el INSERT antes del UPDATE, fallaría.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task El_traslado_cierra_y_abre_en_el_mismo_ciclo_sin_violar_el_indice()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionA = await SembrarDivisionAsync(cursoID, "A");
		var divisionB = await SembrarDivisionAsync(cursoID, "B");
		var alumnoID = await SembrarAlumnoAsync();
		var ciclo = CicloLectivo.Crear("2024");

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(new CursanteBuilder()
				.ConAlumno(alumnoID)
				.ConDivision(divisionA)
				.ConCicloLectivo(ciclo)
				.ConFechaInicio(DateTime.Today.AddDays(-30))
				.Build());
			await contexto.SaveChangesAsync();
		}

		// El mismo camino que ServicioCursante.CambiarDeDivisionAsync.
		using var uow = CrearUnidadDeTrabajo();
		var origen = await uow.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		origen.ShouldNotBeNull();
		origen.DarDeBaja(DateTime.Today);
		uow.Cursantes.Modificar(origen);
		await uow.Cursantes.AgregarAsync(new Cursante(divisionB, alumnoID, ciclo, DateTime.Today, origen.EsRecursante));

		await Should.NotThrowAsync(() => uow.GuardarCambiosAsync());

		using var verificacion = CrearUnidadDeTrabajo();
		var activa = await verificacion.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		activa.ShouldNotBeNull();
		activa.DivisionID.ShouldBe(divisionB);
	}
}
