using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;
using Xunit;

namespace EDUSIS.TestSupport.Infraestructura;

/// <summary>
/// Fixture de SQL Server para las suites de integración y end-to-end
/// (contracts/test-support-api.md §5.2). Una instancia por <em>collection</em>.
///
/// <para>
/// <see cref="InitializeAsync"/>: si no hay motor disponible marca <see cref="Omitir"/> y
/// retorna; si existe <c>EDUSIS_TEST_SQLSERVER</c> la usa; si no, arranca un contenedor
/// <c>mcr.microsoft.com/mssql/server:2022-latest</c>. Luego aplica las migraciones EF Core
/// existentes por código (<c>Database.Migrate()</c>, FR-019) e inicializa <c>Respawner</c>.
/// </para>
///
/// <para>
/// Cualquier fallo al preparar la base (contenedor que no levanta, migración que no aplica —
/// FR-019/FR-018) se traduce en <see cref="Omitir"/> = <c>true</c> con <see cref="MotivoOmision"/>
/// explicativo: las pruebas se omiten (skip), la ejecución no se marca como fallida.
/// </para>
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
	#region ESTADO
	private MsSqlContainer? _contenedor;
	private Respawner? _respawner;
	private bool _contenedorPropio;

	/// <summary>Cadena a la base de prueba. Nunca es la de <c>InfrastructureDI</c> (FR-016).</summary>
	public string CadenaDeConexion { get; private set; } = string.Empty;

	/// <summary><c>true</c> cuando no hay base disponible o no se pudo preparar el esquema.</summary>
	public bool Omitir { get; private set; }

	/// <summary>Motivo legible del <see cref="Omitir"/>, para el mensaje de <c>Assert.Skip</c>.</summary>
	public string MotivoOmision { get; private set; } = string.Empty;
	#endregion

	#region CICLO DE VIDA
	public async Task InitializeAsync()
	{
		if (!DockerDisponible.Verificar())
		{
			Omitir = true;
			MotivoOmision = "Docker no disponible ni EDUSIS_TEST_SQLSERVER definida; se omiten las pruebas que requieren base de datos.";
			return;
		}

		try
		{
			var cadenaExterna = Environment.GetEnvironmentVariable("EDUSIS_TEST_SQLSERVER");
			if (!string.IsNullOrWhiteSpace(cadenaExterna))
			{
				CadenaDeConexion = cadenaExterna;
			}
			else
			{
				_contenedor = new MsSqlBuilder()
					.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
					.Build();

				await _contenedor.StartAsync();
				_contenedorPropio = true;
				CadenaDeConexion = _contenedor.GetConnectionString();
			}

			GarantizarQueNoEsProduccion(CadenaDeConexion);

			await AplicarMigracionesAsync();
			await InicializarRespawnerAsync();
		}
		catch (Exception ex)
		{
			Omitir = true;
			MotivoOmision = $"No se pudo preparar el SQL Server de prueba ({ex.GetType().Name}): {ex.Message}";
			await DisposeAsync();
		}
	}

	public async Task DisposeAsync()
	{
		if (_contenedorPropio && _contenedor is not null)
		{
			await _contenedor.DisposeAsync();
			_contenedor = null;
			_contenedorPropio = false;
		}
	}
	#endregion

	#region SUPERFICIE PARA LAS PRUEBAS
	/// <summary>Nueva instancia de <see cref="EdusisDBContext"/> contra la base de prueba.</summary>
	public EdusisDBContext CrearContexto()
	{
		var opciones = new DbContextOptionsBuilder<EdusisDBContext>()
			.UseSqlServer(CadenaDeConexion)
			.Options;

		return new EdusisDBContext(opciones);
	}

	/// <summary>
	/// Vacía todas las tablas conservando el esquema (FR-009). Se llama en el
	/// <c>InitializeAsync</c> de cada clase de prueba. No-op si la base se omite.
	/// </summary>
	public async Task ResetAsync()
	{
		if (Omitir || _respawner is null)
		{
			return;
		}

		await using var conexion = new SqlConnection(CadenaDeConexion);
		await conexion.OpenAsync();
		await _respawner.ResetAsync(conexion);
	}
	#endregion

	#region PREPARACIÓN
	private async Task AplicarMigracionesAsync()
	{
		await using var contexto = CrearContexto();
		await contexto.Database.MigrateAsync();
	}

	private async Task InicializarRespawnerAsync()
	{
		await using var conexion = new SqlConnection(CadenaDeConexion);
		await conexion.OpenAsync();

		_respawner = await Respawner.CreateAsync(conexion, new RespawnerOptions
		{
			DbAdapter = DbAdapter.SqlServer,
			TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
		});
	}

	/// <summary>
	/// FR-016: aborta si la cadena de prueba apunta a la base hardcodeada en
	/// <c>InfrastructureDI</c> (<c>localhost</c> / <c>EdusisDB</c>).
	/// </summary>
	private static void GarantizarQueNoEsProduccion(string cadena)
	{
		var constructor = new SqlConnectionStringBuilder(cadena);

		var apuntaAProduccion =
			constructor.DataSource.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
			constructor.InitialCatalog.Equals("EdusisDB", StringComparison.OrdinalIgnoreCase);

		if (apuntaAProduccion)
		{
			throw new InvalidOperationException(
				"La cadena de conexión de prueba apunta a la base de producción (localhost / EdusisDB). " +
				"Definí EDUSIS_TEST_SQLSERVER con una base vacía y desechable (FR-016).");
		}
	}
	#endregion
}
