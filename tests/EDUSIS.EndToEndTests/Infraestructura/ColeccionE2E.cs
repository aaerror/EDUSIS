using System.Threading;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EDUSIS.EndToEndTests.Infraestructura;

/// <summary>
/// Nombre de la <em>collection</em> que comparte la única instancia de
/// <see cref="SqlServerFixture"/> entre todas las clases end-to-end. Es una colección aparte de
/// la de integración (US3): cada suite levanta su propio contenedor / su propia base.
/// </summary>
public static class ColeccionE2E
{
	public const string Nombre = "E2E SqlServer";
}

/// <summary>
/// Define la <em>collection</em> de xUnit para la suite end-to-end: una sola
/// <see cref="SqlServerFixture"/> (un contenedor / una base) reutilizada por todas las clases de
/// flujos (FR-009).
/// </summary>
[CollectionDefinition(ColeccionE2E.Nombre)]
public sealed class DefinicionColeccionE2E : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// Base de toda clase de prueba end-to-end. Lleva la categoría <c>E2E</c> y se une a la
/// <em>collection</em> de SQL Server de esta suite.
/// <list type="bullet">
///   <item>Las pruebas se anotan con <c>[RequiereSqlServerFact]</c>: sin Docker ni
///   <c>EDUSIS_TEST_SQLSERVER</c> quedan <em>Skipped</em> (salida 0), no fallidas
///   (contracts/test-execution.md §5).</item>
///   <item>Si el motor está disponible pero el esquema no se pudo preparar (contenedor que no
///   levanta, migración que no aplica — FR-019), <see cref="InitializeAsync"/> corta con un
///   error explícito: es un defecto que bloquea la suite y debe quedar en <c>hallazgos.md</c>
///   (FR-018).</item>
///   <item>Antes de cada prueba resetea los datos con Respawn (FR-009) y arma una composición
///   de dependencias real fresca (<see cref="ComposicionDePruebaE2E"/>).</item>
/// </list>
/// </summary>
[Trait("Categoria", Categorias.E2E)]
[Collection(ColeccionE2E.Nombre)]
public abstract class BaseE2E : IAsyncLifetime
{
	private ServiceProvider? _composicion;

	protected SqlServerFixture Fixture { get; }

	protected BaseE2E(SqlServerFixture fixture)
	{
		Fixture = fixture;
	}

	public async Task InitializeAsync()
	{
		// Sin motor disponible, `[RequiereSqlServerFact]` ya omitió la prueba en descubrimiento;
		// este `InitializeAsync` ni siquiera corre. Si llegamos acá con `Omitir`, es porque el
		// motor está pero el esquema falló: se corta con un mensaje que apunta al defecto (FR-018).
		if (Fixture.Omitir)
		{
			throw new InvalidOperationException(Fixture.MotivoOmision);
		}

		await Fixture.ResetAsync();
		_composicion = ComposicionDePruebaE2E.Construir(Fixture.CadenaDeConexion);
	}

	public Task DisposeAsync()
	{
		_composicion?.Dispose();
		return Task.CompletedTask;
	}

	#region SCOPES DE LA COMPOSICIÓN REAL

	/// <summary>
	/// Ejecuta <paramref name="operacion"/> dentro de un <see cref="IServiceScope"/> nuevo, como
	/// cada request de la app en producción. Resolver <c>IServicio&lt;X&gt;</c> del
	/// <see cref="IServiceProvider"/> recibido.
	/// </summary>
	protected async Task EnUnScope(Func<IServiceProvider, Task> operacion)
	{
		using var scope = _composicion!.CreateScope();
		await operacion(scope.ServiceProvider);
	}

	/// <summary>Variante que devuelve un valor (p. ej. un <c>Response</c> de consulta).</summary>
	protected async Task<T> EnUnScope<T>(Func<IServiceProvider, Task<T>> operacion)
	{
		using var scope = _composicion!.CreateScope();
		return await operacion(scope.ServiceProvider);
	}

	#endregion

	#region SEMBRADO DE AGREGADOS PADRE (FK reales)

	// Los agregados con FK real (Licencia → docente, Usuario → docente) necesitan que la fila
	// referenciada exista. Este contador garantiza legajos / CUIL / documentos únicos entre
	// pruebas de la misma corrida (índices únicos en `persona`), aunque Respawn resetee entre
	// pruebas.
	private static int _secuencia = 500_000;

	private static string SiguienteNumero() =>
		Interlocked.Increment(ref _secuencia).ToString("D6");

	/// <summary>Persiste un <see cref="Domain.Docentes.Docente"/> válido directamente y lo devuelve.</summary>
	protected async Task<Domain.Docentes.Docente> SembrarDocenteAsync(Action<DocenteBuilder>? configurar = null)
	{
		var numero = SiguienteNumero();

		var builder = new DocenteBuilder()
			.ConLegajo(numero)
			.ConCuil("20" + numero + "00" + numero[..1])
			.ConEmail($"docente.{numero}@correo.com")
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConDocumento("3" + numero + "0")
				.ConEdad(40));

		configurar?.Invoke(builder);
		var docente = builder.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(docente);
		await contexto.SaveChangesAsync();
		return docente;
	}

	/// <summary>Persiste un <see cref="Domain.Cursos.Curso"/> con la cantidad de divisiones indicada y lo devuelve.</summary>
	protected async Task<Domain.Cursos.Curso> SembrarCursoAsync(string grado = "Primero", int divisiones = 0)
	{
		var curso = new CursoBuilder()
			.ConGrado(grado)
			.ConNivelEducativo("Secundaria")
			.ConDivision(divisiones)
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(curso);
		await contexto.SaveChangesAsync();
		return curso;
	}

	#endregion
}
