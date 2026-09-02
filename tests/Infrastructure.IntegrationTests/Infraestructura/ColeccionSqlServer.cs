using System.Threading;
using Domain.Cursos;
using Domain.Docentes;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Infraestructura;

/// <summary>
/// Nombre de la <em>collection</em> que comparte la única instancia de
/// <see cref="SqlServerFixture"/> entre todas las clases de integración.
/// </summary>
public static class ColeccionSqlServer
{
	public const string Nombre = "SqlServer";
}

/// <summary>
/// Define la <em>collection</em> de xUnit: una sola <see cref="SqlServerFixture"/> (un
/// contenedor / una base) para toda la suite de integración (contracts/test-execution.md §5,
/// FR-009).
/// </summary>
[CollectionDefinition(ColeccionSqlServer.Nombre)]
public sealed class DefinicionColeccionSqlServer : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// Base de toda clase de prueba de integración. Lleva la categoría <c>Integracion</c> y se
/// une a la <em>collection</em> de SQL Server.
/// <list type="bullet">
///   <item>Las pruebas se anotan con <c>[RequiereSqlServerFact]</c>: sin Docker ni
///   <c>EDUSIS_TEST_SQLSERVER</c> quedan <em>Skipped</em> (salida 0), no fallidas
///   (contracts/test-execution.md §5).</item>
///   <item>Si el motor está disponible pero el esquema no se pudo preparar (contenedor que no
///   levanta, migración que no aplica — FR-019), <see cref="InitializeAsync"/> corta con un
///   error explícito: es un defecto que bloquea la suite y debe quedar en <c>hallazgos.md</c>
///   (FR-018).</item>
///   <item>Antes de cada prueba resetea los datos con Respawn (FR-009).</item>
/// </list>
/// </summary>
[Trait("Categoria", Categorias.Integracion)]
[Collection(ColeccionSqlServer.Nombre)]
public abstract class BaseIntegracion : IAsyncLifetime
{
	protected SqlServerFixture Fixture { get; }

	protected BaseIntegracion(SqlServerFixture fixture)
	{
		Fixture = fixture;
	}

	public async Task InitializeAsync()
	{
		// Sin motor disponible, `[RequiereSqlServerFact]` ya omitió la prueba en descubrimiento;
		// este `InitializeAsync` ni siquiera corre. Si llegamos acá con `Omitir`, es porque el
		// motor está pero el esquema falló: se corta con un mensaje que apunta al defecto.
		if (Fixture.Omitir)
		{
			throw new InvalidOperationException(Fixture.MotivoOmision);
		}

		await Fixture.ResetAsync();
	}

	public Task DisposeAsync() => Task.CompletedTask;

	#region HELPERS DE COMPOSICIÓN

	/// <summary>
	/// <c>IMediator</c> real, con MediatR escaneando el assembly de <c>Infrastructure</c>
	/// (igual que <c>InfrastructureDI</c>: <c>RegisterServicesFromAssemblyContaining&lt;EdusisDBContext&gt;()</c>).
	/// </summary>
	protected static IMediator CrearMediator() =>
		new ServiceCollection()
			.AddMediatR(configuracion => configuracion.RegisterServicesFromAssemblyContaining<EdusisDBContext>())
			.BuildServiceProvider()
			.GetRequiredService<IMediator>();

	/// <summary>
	/// <see cref="UnitOfWork"/> real contra la base de prueba. Expone los repositorios
	/// concretos (algunos <c>internal</c>) por sus interfaces. Cada llamada abre un
	/// <see cref="EdusisDBContext"/> nuevo; el <c>UnitOfWork</c> lo posee y lo libera.
	/// </summary>
	protected UnitOfWork CrearUnidadDeTrabajo() =>
		new(Fixture.CrearContexto(), CrearMediator());

	#endregion

	#region SEMBRADO DE AGREGADOS PADRE

	// Los agregados con FK real (Licencia → docente, Usuario → docente, Curricula → curso)
	// necesitan que la fila referenciada exista. Este contador garantiza legajos / CUIL /
	// documentos únicos entre pruebas de la misma corrida (índices únicos en `persona`).
	private static int _secuencia = 100_000;

	private static string SiguienteNumero() =>
		Interlocked.Increment(ref _secuencia).ToString("D6");

	/// <summary>Persiste un <see cref="Docente"/> válido y devuelve su <c>Id</c>.</summary>
	protected async Task<Guid> SembrarDocenteAsync()
	{
		var numero = SiguienteNumero();

		var docente = new DocenteBuilder()
			.ConLegajo(numero)
			.ConCuil("20" + numero + "00" + numero[..1])
			.ConEmail($"docente.{numero}@correo.com")
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConDocumento("3" + numero + "0")
				.ConEdad(40))
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(docente);
		await contexto.SaveChangesAsync();
		return docente.Id;
	}

	/// <summary>Persiste un <see cref="Curso"/> con la cantidad de divisiones indicada y devuelve su <c>Id</c>.</summary>
	protected async Task<Guid> CrearCursoPersistidoAsync(string grado = "Primero", int divisiones = 0)
	{
		var curso = new CursoBuilder()
			.ConGrado(grado)
			.ConNivelEducativo("Secundaria")
			.ConDivision(divisiones)
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(curso);
		await contexto.SaveChangesAsync();
		return curso.Id;
	}

	#endregion
}
