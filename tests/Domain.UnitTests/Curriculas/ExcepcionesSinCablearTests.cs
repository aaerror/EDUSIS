using EDUSIS.TestSupport;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Excepciones del módulo <c>Curriculas</c> definidas pero que ningún constructor ni método del
/// dominio lanza (código sin cablear o dentro de bloques comentados). Cada una queda cubierta
/// con una prueba <c>Skip</c> y su hallazgo en <c>hallazgos.md</c> (H-010, H-011, H-012).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ExcepcionesSinCablearTests
{
	[Fact(Skip = "H-010: DocenteSinCargoException no se lanza desde ningún método del dominio. Ver hallazgos.md.")]
	public void Operar_sobre_una_materia_sin_docente_lanza_DocenteSinCargoException()
	{
	}

	[Fact(Skip = "H-011: LimiteCantidadHorasCatedrasException no se lanza; la validación de límite de horas cátedra está en código comentado. Ver hallazgos.md.")]
	public void Superar_el_limite_de_horas_catedra_lanza_LimiteCantidadHorasCatedrasException()
	{
	}

	[Fact(Skip = "H-012: MateriaSinHorariosAsignadoException sólo aparece en el bloque de horarios comentado de Materia. Ver hallazgos.md.")]
	public void Cambiar_el_horario_de_una_materia_sin_horarios_lanza_MateriaSinHorariosAsignadoException()
	{
	}
}
