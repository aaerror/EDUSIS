using Domain.Curriculas;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Agregado <see cref="Curricula"/>: alta y vigencia del período. Las materias y las cátedras
/// (materia + división, con los cargos docentes) pasaron a ser agregados propios
/// (<c>Domain.Materias.Materia</c>, <c>Domain.Catedras.Catedra</c>): su cobertura vive en
/// <c>MateriaTests</c>, <c>CatedraTests</c> y <c>SituacionRevistaTests</c>. Las excepciones del
/// módulo son <c>internal</c>: se verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CurriculaTests
{
	#region Alta y vigencia
	[Fact]
	public void Una_curricula_nueva_guarda_el_curso_y_arranca_vigente()
	{
		var cursoID = Guid.NewGuid();

		var curricula = new CurriculaBuilder().ConCurso(cursoID).Build();

		curricula.CursoID.ShouldBe(cursoID);
		curricula.EstaVigente().ShouldBeTrue();
	}

	[Fact]
	public void Desafectar_le_pone_fecha_de_fin_al_periodo()
	{
		var curricula = new CurriculaBuilder().Build();

		curricula.Desafectar();

		curricula.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void EstablecerFechaFinalizacion_sobre_una_curricula_ya_expirada_lanza_CurriculaNoVigenteException()
	{
		var curricula = new CurriculaBuilder()
			.ConFechaInicio(new DateTime(2020, 1, 1))
			.ConFechaFin(new DateTime(2020, 12, 31))
			.Build();

		var ex = Should.Throw<Exception>(() => curricula.EstablecerFechaFinalizacion(DateTime.Today));

		ex.GetType().Name.ShouldBe("CurriculaNoVigenteException");
	}
	#endregion
}
