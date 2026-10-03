using EDUSIS.TestSupport;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Excepciones del módulo <c>Curriculas</c> definidas pero que ningún constructor ni método del
/// dominio lanza (código sin cablear o dentro de bloques comentados). Cada una queda cubierta
/// con una prueba <c>Skip</c> y su hallazgo en <c>hallazgos.md</c> (H-010, H-011). H-012
/// (<c>MateriaSinHorariosAsignadoException</c>) se eliminó junto con el refactor de agregados
/// de <c>002-fix-dominio</c>: ver la anotación actualizada en <c>hallazgos.md</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ExcepcionesSinCablearTests
{
	// H-010 quedó corregido por el rediseño de agregados: `Catedra.RelevarDeFunciones` lanza
	// `DocenteSinCargoException` (`Domain/Catedras/Catedra.cs:136`) y la cubre una prueba real,
	// `CatedraTests.RelevarDeFunciones_sin_nadie_en_funciones_lanza_DocenteSinCargoException`.
	// El placeholder vacío que lo documentaba se retiró: afirmaba lo contrario de lo que el código hace.

	[Fact(Skip = "H-011: LimiteCantidadHorasCatedrasException no se lanza; la validación de límite de horas cátedra está en código comentado. Ver hallazgos.md.")]
	public void Superar_el_limite_de_horas_catedra_lanza_LimiteCantidadHorasCatedrasException()
	{
	}
}
