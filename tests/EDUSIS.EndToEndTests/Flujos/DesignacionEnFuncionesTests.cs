using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Domain.Catedras.Exceptions;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// <see cref="IServicioCatedra.DesignarYPonerEnFuncionesAsync"/> contra SQL Server real: los dos
/// <c>SaveChanges</c> que exige el ciclo de FK entre <c>catedra</c> y <c>situacion_revista</c>
/// quedan dentro de una transacción. El fake de <c>Core.UnitTests</c> sólo registra la secuencia
/// Begin/Commit/Rollback; acá se ve si la base conserva o no la designación.
/// </summary>
public sealed class DesignacionEnFuncionesTests : BaseE2E
{
	public DesignacionEnFuncionesTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Designar_y_poner_en_funciones_persiste_las_dos_cosas()
	{
		var (catedraID, docenteID) = await SembrarCatedraYDocenteAsync();

		var situacionID = await EnUnScope(sp => sp.GetRequiredService<IServicioCatedra>().DesignarYPonerEnFuncionesAsync(
			new DesignarDocenteRequest(catedraID, docenteID, "Titular", DateTime.Today, null, null)));

		await using var contexto = Fixture.CrearContexto();
		var catedra = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == catedraID);
		catedra.SituacionEnFuncionesID.ShouldBe(situacionID);
		catedra.DocenteEnFunciones().ShouldBe(docenteID);
	}

	/// <remarks>
	/// La designación con inicio futuro se acepta, pero no puede estar en el aula todavía: falla
	/// el segundo paso. Con dos casos de uso separados la designación quedaba guardada.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task Si_poner_en_funciones_falla_la_designacion_no_queda_en_la_base()
	{
		var (catedraID, docenteID) = await SembrarCatedraYDocenteAsync();

		await Should.ThrowAsync<CargoNoVigenteException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioCatedra>().DesignarYPonerEnFuncionesAsync(
				new DesignarDocenteRequest(catedraID, docenteID, "Titular", DateTime.Today.AddDays(5), null, null))));

		await using var contexto = Fixture.CrearContexto();
		var catedra = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == catedraID);
		catedra.SituacionesRevista.ShouldBeEmpty();
		catedra.SituacionEnFuncionesID.ShouldBeNull();
	}

	private async Task<(Guid CatedraID, Guid DocenteID)> SembrarCatedraYDocenteAsync()
	{
		var curso = await SembrarCursoAsync();
		var division = await SembrarDivisionAsync(curso.Id);
		var docente = await SembrarDocenteAsync();

		await using var contexto = Fixture.CrearContexto();
		var curricula = new CurriculaBuilder().ConCurso(curso.Id).ConFechaInicio(DateTime.Today.AddDays(-30)).Build();
		contexto.Add(curricula);
		await contexto.SaveChangesAsync();

		var materia = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Matemática").ConHorasCatedra(4).Build();
		contexto.Add(materia);
		await contexto.SaveChangesAsync();

		var catedra = new CatedraBuilder().ConMateria(materia.Id).ConDivision(division.Id).Build();
		contexto.Add(catedra);
		await contexto.SaveChangesAsync();

		return (catedra.Id, docente.Id);
	}
}
