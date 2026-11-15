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
/// flujos.
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
///   <c>EDUSIS_TEST_SQLSERVER</c> quedan <em>Skipped</em> (salida 0), no fallidas.</item>
///   <item>Si el motor está disponible pero el esquema no se pudo preparar (contenedor que no
///   levanta, migración que no aplica), <see cref="InitializeAsync"/> corta con un
///   error explícito: es un defecto que bloquea la suite.</item>
///   <item>Antes de cada prueba resetea los datos con Respawn y arma una composición
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
		// motor está pero el esquema falló: se corta con un mensaje que apunta al defecto.
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

	/// <summary>Persiste un <see cref="Domain.Cursos.Curso"/> y lo devuelve (las divisiones son agregados propios: usar <see cref="SembrarDivisionAsync"/>).</summary>
	protected async Task<Domain.Cursos.Curso> SembrarCursoAsync(string grado = "Primero")
	{
		var curso = new CursoBuilder()
			.ConGrado(grado)
			.ConNivelEducativo("Secundaria")
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(curso);
		await contexto.SaveChangesAsync();
		return curso;
	}

	/// <summary>Persiste un <see cref="Domain.Alumnos.Alumno"/> válido directamente y lo devuelve.</summary>
	/// <remarks>
	/// Lo necesita todo flujo que inscriba: <c>FK_CURSANTE_ALUMNO</c> apunta a <c>alumno</c>, así que
	/// un identificador suelto hace fallar el <c>INSERT</c> del cursante. El legajo, el documento y
	/// el correo salen del contador de la clase porque <c>persona</c> tiene índices únicos sobre los
	/// tres y Respawn no reinicia la secuencia entre pruebas.
	/// </remarks>
	protected async Task<Domain.Alumnos.Alumno> SembrarAlumnoAsync()
	{
		var numero = SiguienteNumero();

		var alumno = new AlumnoBuilder()
			.ConLegajo("AL" + numero[..4])
			.ConEmail($"alumno.{numero}@correo.com")
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConDocumento("2" + numero + "0")
				.ConEdad(15))
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(alumno);
		await contexto.SaveChangesAsync();
		return alumno;
	}

	/// <summary>Persiste una <see cref="Domain.Divisiones.Division"/> propia del curso indicado y la devuelve.</summary>
	protected async Task<Domain.Divisiones.Division> SembrarDivisionAsync(Guid cursoID, string descripcion = "A")
	{
		var division = new DivisionBuilder()
			.ConCurso(cursoID)
			.ConDescripcion(descripcion)
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(division);
		await contexto.SaveChangesAsync();
		return division;
	}

	#endregion
}
