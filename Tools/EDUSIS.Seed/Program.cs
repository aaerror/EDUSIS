using Bogus;
using Core.Shared;
using Core.Shared.Seguridad;
using EDUSIS.Seed;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Semilla fija y GLOBAL de Bogus: el dataset tiene que ser reproducible. Si cada corrida
// generara alumnos distintos no se podría reportar un defecto de interfaz contra datos
// concretos ("el domicilio de Pérez se corta en la columna") ni verificar que se arregló.
Randomizer.Seed = new Random(20261013);

var limpiar = args.Contains("--limpiar", StringComparer.OrdinalIgnoreCase);
var siNoPreguntar = args.Contains("--si", StringComparer.OrdinalIgnoreCase);

var servicios = new ServiceCollection();

servicios.AddLogging(configuracion =>
	{
		configuracion.AddSimpleConsole(opciones => opciones.SingleLine = true);
		configuracion.SetMinimumLevel(LogLevel.Information);

		// EF loguea cada comando SQL en Information: sembrando miles de filas tapa el informe
		// de la corrida. El filtro alcanza porque InfrastructureDI ya no cablea un LogTo a la
		// consola por fuera del ILoggerFactory; si alguien lo vuelve a poner, este filtro deja de
		// tener efecto sin ningún error.
		configuracion.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
	});

servicios.AddInfrastructure();

// La sesión del seeder, registrada por SU composition root y no por CoreDI (ver
// SesionDeSistema y la decisión de la Fase 5). AddServices() se llama sólo para que
// IServicioSeguridad —que es quien hashea las contraseñas de los usuarios— se resuelva del
// grafo real en lugar de reimplementarse acá.
servicios.AddSingleton<ISesionActual, SesionDeSistema>();
servicios.AddServices();

servicios.AddScoped<Sembradora>();

using var proveedor = servicios.BuildServiceProvider();
using var ambito = proveedor.CreateScope();

var registrador = ambito.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EDUSIS.Seed");
var contexto = ambito.ServiceProvider.GetRequiredService<EdusisDBContext>();

try
{
	#region Comprobaciones previas
	if (!await contexto.Database.CanConnectAsync())
	{
		registrador.LogError(
			"No se pudo conectar a la base. Revisá la cadena hardcodeada en Infrastructure/InfrastructureDI.cs y que la migración esté aplicada.");
		return 1;
	}

	var pendientes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();
	if (pendientes.Count > 0)
	{
		registrador.LogError(
			"Hay {Cantidad} migración/es sin aplicar ({Nombres}). Corré `dotnet ef database update --project Infrastructure --startup-project WPF_Desktop` antes de sembrar.",
			pendientes.Count,
			string.Join(", ", pendientes));
		return 1;
	}

	var yaTieneDatos = await contexto.Cursos.AnyAsync()
					|| await contexto.Docentes.AnyAsync()
					|| await contexto.Alumnos.AnyAsync();

	if (yaTieneDatos && !limpiar)
	{
		registrador.LogError(
			"La base ya tiene datos de demo. Corré con --limpiar para borrarlos y volver a sembrar, o sembrá sobre una base limpia.");
		return 1;
	}

	if (yaTieneDatos && limpiar)
	{
		if (!siNoPreguntar && !Confirmar())
		{
			registrador.LogInformation("Cancelado. No se borró nada.");
			return 130;
		}

		await Limpiador.LimpiarAsync(contexto, registrador);
	}
	#endregion

	var sembradora = ambito.ServiceProvider.GetRequiredService<Sembradora>();

	var cronometro = System.Diagnostics.Stopwatch.StartNew();
	var resumen = await sembradora.SembrarAsync();
	cronometro.Stop();

	Informar(resumen, cronometro.Elapsed);
	return 0;
}
catch (Exception excepcion)
{
	registrador.LogError(excepcion, "La siembra falló y se interrumpió.");
	return 1;
}

/// <summary>
/// Pide confirmación antes de borrar. El seeder no borra datos sin que alguien lo diga en voz
/// alta: <c>--limpiar</c> expresa la intención, esto confirma que la base es la que se cree.
/// </summary>
static bool Confirmar()
{
	Console.WriteLine();
	Console.WriteLine("  ATENCIÓN: se van a BORRAR todos los datos de demo de la base EdusisDB.");
	Console.WriteLine("  Se conservan los datos de referencia (rol, ciclo_lectivo) y el esquema.");
	Console.WriteLine();
	Console.Write("  Escribí 'borrar' para continuar: ");

	return string.Equals(Console.ReadLine()?.Trim(), "borrar", StringComparison.OrdinalIgnoreCase);
}

static void Informar(ResumenDeSiembra resumen, TimeSpan duracion)
{
	Console.WriteLine();
	Console.WriteLine("  Escuela demo sembrada");
	Console.WriteLine("  ─────────────────────");
	Console.WriteLine($"  Docentes . . . . . . {resumen.Docentes,6}");
	Console.WriteLine($"  Alumnos  . . . . . . {resumen.Alumnos,6}");
	Console.WriteLine($"  Cursos . . . . . . . {resumen.Cursos,6}");
	Console.WriteLine($"  Currículas vigentes  {resumen.Curriculas,6}");
	Console.WriteLine($"  Materias . . . . . . {resumen.Materias,6}");
	Console.WriteLine($"  Divisiones . . . . . {resumen.Divisiones,6}");
	Console.WriteLine($"  Cátedras . . . . . . {resumen.Catedras,6}");
	Console.WriteLine($"  Horas con horario  . {resumen.HorariosAsignados,6}");
	Console.WriteLine($"  Horas sin asignar  . {resumen.HorariosSinAsignar,6}");
	Console.WriteLine($"  Cursantes  . . . . . {resumen.Cursantes,6}");
	Console.WriteLine($"  Calificaciones . . . {resumen.Calificaciones,6}");
	Console.WriteLine($"  Planillas  . . . . . {resumen.Planillas,6}");
	Console.WriteLine($"  Usuarios . . . . . . {resumen.Usuarios,6}");
	Console.WriteLine("  ─────────────────────");
	Console.WriteLine($"  Duración: {duracion.TotalSeconds:F1} s");
	Console.WriteLine();
	// Este listado se escribe a mano y no sale de los datos sembrados: las descripciones son
	// prosa, no se derivan de Rol.Permisos. Si cambia la atribución de Domain/Usuarios/Rol.cs o
	// el array `cuentas` de Sembradora.SembrarUsuariosAsync, hay que tocar las seis líneas de
	// acá abajo también. Ya quedó desactualizado una vez, al partir los permisos por actor.
	Console.WriteLine($"  Usuarios (contraseña: {Sembradora.PASSWORD_DEMO})");
	Console.WriteLine("    admin@edu.ar        — Administrador (los quince permisos)");
	Console.WriteLine("    direccion@edu.ar    — Dirección (los nueve académicos)");
	Console.WriteLine("    secretaria@edu.ar   — Secretaría (preceptores, documentos, reportes)");
	Console.WriteLine("    academica@edu.ar    — Secretaría Académica (divisiones, cursantes, cátedras)");
	Console.WriteLine("    preceptoria@edu.ar  — Preceptoría (asistencia y reportes de cursada)");
	Console.WriteLine("    docente@edu.ar      — Docente (calificaciones)");
	Console.WriteLine();
}
