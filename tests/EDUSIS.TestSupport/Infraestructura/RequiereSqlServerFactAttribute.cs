using Xunit;

namespace EDUSIS.TestSupport.Infraestructura;

/// <summary>
/// <c>[Fact]</c> que se marca como <em>Skipped</em> —en tiempo de descubrimiento— cuando no
/// hay un motor SQL Server disponible para la prueba (ni demonio Docker ni
/// <c>EDUSIS_TEST_SQLSERVER</c>).
///
/// <para>
/// xUnit 2.9 no ofrece <c>Assert.Skip</c> dinámico (es de v3) y el token de omisión
/// dinámica de v2 no lo respeta este runner, así que la decisión de omitir se toma acá, al
/// construir el atributo. Cubre <c>contracts/test-execution.md</c> §5 y
/// <c>contracts/test-support-api.md</c> §6 (<c>[FactRequiereSqlServer]</c>).
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiereSqlServerFactAttribute : FactAttribute
{
	public RequiereSqlServerFactAttribute()
	{
		if (!DockerDisponible.Verificar())
		{
			Skip = "Sin Docker ni EDUSIS_TEST_SQLSERVER: se omiten las pruebas que requieren SQL Server (contracts/test-execution.md §5).";
		}
	}
}
