using Domain.Cursantes;
using Domain.Shared.Exceptions;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// Concurrencia optimista por agregado (columna <c>version</c> en cada raíz). Cada
/// <see cref="UnitOfWork"/> es una pantalla de una estación distinta: las dos leen el mismo
/// agregado, la primera guarda, y la segunda tiene que enterarse en vez de pisarla.
/// </summary>
public sealed class ConcurrenciaOptimistaTests : BaseIntegracion
{
	public ConcurrenciaOptimistaTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Dos_estaciones_que_modifican_la_misma_raiz_la_segunda_recibe_el_conflicto()
	{
		var (alumnoID, ciclo, _) = await SembrarInscripcionAsync();

		using var primera = CrearUnidadDeTrabajo();
		using var segunda = CrearUnidadDeTrabajo();
		var enPrimera = await primera.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		var enSegunda = await segunda.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);

		enPrimera!.ModificarCondicionDeRecursante(true);
		await primera.GuardarCambiosAsync();

		// Respecto de lo que leyó la segunda (false) también es un cambio: hay UPDATE y el WHERE compara la versión.
		enSegunda!.ModificarCondicionDeRecursante(true);
		await Should.ThrowAsync<ConflictoDeConcurrenciaException>(() => segunda.GuardarCambiosAsync());
	}

	/// <summary>
	/// Agregar una calificación es un INSERT en <c>calificacion</c>: sin marcar la raíz, la fila de
	/// <c>cursante</c> no cambia y la otra estación no se entera.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task Un_cambio_en_una_entidad_interna_avanza_la_version_de_la_raiz()
	{
		var (alumnoID, ciclo, materiaID) = await SembrarInscripcionAsync();

		using var primera = CrearUnidadDeTrabajo();
		using var segunda = CrearUnidadDeTrabajo();
		var enPrimera = await primera.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		var enSegunda = await segunda.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);

		enPrimera!.RegistrarCalificacion(materiaID, DateTime.Today, "PrimerCuatrimestre", false, 8, null);
		primera.Cursantes.Modificar(enPrimera);
		await primera.GuardarCambiosAsync();

		enSegunda!.ModificarCondicionDeRecursante(true);
		await Should.ThrowAsync<ConflictoDeConcurrenciaException>(() => segunda.GuardarCambiosAsync());
	}

	/// <summary>
	/// La dirección opuesta: la segunda estación SÓLO toca una entidad interna, así que es
	/// <c>UnitOfWork</c> quien tiene que llevar su raíz al UPDATE para que el WHERE compare la versión.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task Un_cambio_solo_en_una_entidad_interna_detecta_el_conflicto_con_la_raiz()
	{
		var (alumnoID, ciclo, materiaID) = await SembrarInscripcionAsync();

		using var primera = CrearUnidadDeTrabajo();
		using var segunda = CrearUnidadDeTrabajo();
		var enPrimera = await primera.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);
		var enSegunda = await segunda.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);

		enPrimera!.ModificarCondicionDeRecursante(true);
		await primera.GuardarCambiosAsync();

		enSegunda!.RegistrarCalificacion(materiaID, DateTime.Today, "PrimerCuatrimestre", false, 8, null);
		segunda.Cursantes.Modificar(enSegunda);
		await Should.ThrowAsync<ConflictoDeConcurrenciaException>(() => segunda.GuardarCambiosAsync());
	}

	/// <summary>
	/// <c>Alumno</c> es TPT sobre <c>persona</c>, que es la tabla que lleva la versión. Desinscribir
	/// sólo escribe columnas de <c>alumno</c>; si eso no avanzara la versión de <c>persona</c>, un
	/// cambio concurrente del email pasaría sin conflicto.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task Un_cambio_solo_en_la_tabla_derivada_TPT_avanza_la_version()
	{
		var alumnoID = await SembrarAlumnoAsync();

		using var primera = CrearUnidadDeTrabajo();
		using var segunda = CrearUnidadDeTrabajo();
		var enPrimera = await primera.Alumnos.BuscarPorIDAsync(alumnoID);
		var enSegunda = await segunda.Alumnos.BuscarPorIDAsync(alumnoID);

		enPrimera!.Desinscribir();
		primera.Alumnos.Modificar(enPrimera);
		await primera.GuardarCambiosAsync();

		enSegunda!.CambiarEmail("otro.correo@correo.com");
		segunda.Alumnos.Modificar(enSegunda);
		await Should.ThrowAsync<ConflictoDeConcurrenciaException>(() => segunda.GuardarCambiosAsync());
	}

	/// <summary>Sin falsos positivos: la misma pantalla guarda dos veces seguidas sobre el mismo agregado.</summary>
	[RequiereSqlServerFact]
	public async Task La_misma_estacion_puede_guardar_dos_veces_seguidas()
	{
		var (alumnoID, ciclo, materiaID) = await SembrarInscripcionAsync();

		using var uow = CrearUnidadDeTrabajo();
		var cursante = await uow.Cursantes.BuscarInscripcionActivaAsync(alumnoID, ciclo);

		cursante!.ModificarCondicionDeRecursante(true);
		await uow.GuardarCambiosAsync();

		cursante.RegistrarCalificacion(materiaID, DateTime.Today, "PrimerCuatrimestre", false, 8, null);
		uow.Cursantes.Modificar(cursante);
		await Should.NotThrowAsync(() => uow.GuardarCambiosAsync());
	}

	private async Task<(Guid AlumnoID, CicloLectivo Ciclo, Guid MateriaID)> SembrarInscripcionAsync()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var curriculaID = await SembrarCurriculaAsync(cursoID);
		var materiaID = await SembrarMateriaAsync(curriculaID);
		var alumnoID = await SembrarAlumnoAsync();
		var ciclo = CicloLectivo.Crear("2024");

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(new CursanteBuilder()
			.ConAlumno(alumnoID)
			.ConDivision(divisionID)
			.ConCicloLectivo(ciclo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build());
		await contexto.SaveChangesAsync();

		return (alumnoID, ciclo, materiaID);
	}
}
