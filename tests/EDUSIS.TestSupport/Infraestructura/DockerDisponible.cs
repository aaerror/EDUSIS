using System.Diagnostics;

namespace EDUSIS.TestSupport.Infraestructura;

/// <summary>
/// Sonda de disponibilidad de un motor SQL Server para las suites de integración y
/// end-to-end (contracts/test-support-api.md §5.1). El resultado se cachea: la sonda se
/// evalúa una sola vez por proceso de prueba.
/// </summary>
public static class DockerDisponible
{
	#region SONDA CACHEADA
	private static readonly Lazy<bool> _resultado = new(Sondear);

	/// <summary>
	/// <c>true</c> si existe la variable <c>EDUSIS_TEST_SQLSERVER</c> (SQL Server externo)
	/// o responde el demonio Docker local. <c>false</c> en cualquier otro caso: las suites
	/// de integración/e2e se omiten (skip), no fallan.
	/// </summary>
	public static bool Verificar() => _resultado.Value;
	#endregion

	#region IMPLEMENTACIÓN
	private static bool Sondear()
	{
		if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EDUSIS_TEST_SQLSERVER")))
		{
			return true;
		}

		return DemonioDockerResponde();
	}

	private static bool DemonioDockerResponde()
	{
		try
		{
			using var proceso = Process.Start(new ProcessStartInfo
			{
				FileName = "docker",
				Arguments = "info",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			});

			if (proceso is null)
			{
				return false;
			}

			if (!proceso.WaitForExit(milliseconds: 10_000))
			{
				try { proceso.Kill(entireProcessTree: true); } catch { /* best effort */ }
				return false;
			}

			return proceso.ExitCode == 0;
		}
		catch
		{
			// No hay binario `docker` en el PATH, o el proceso no pudo lanzarse.
			return false;
		}
	}
	#endregion
}
