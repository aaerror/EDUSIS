using Core.ServicioDocentes.Exceptions;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using Domain.Catedras.SituacionesRevista;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 3: solicitud de licencia de un docente desde la fachada
/// <see cref="IServicioLicencia.SolicitarLicencia"/> hasta SQL Server real, con la composición de
/// dependencias de producción.
///
/// <para>
/// La solicitud no tiene efecto sobre otros agregados; la aprobación sí: activa la licencia y
/// <c>LicenciaActivadaEventHandler</c> releva al docente de las cátedras donde está en funciones.
/// </para>
/// </summary>
public sealed class LicenciaDeDocenteTests : BaseE2E
{
	public LicenciaDeDocenteTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task SolicitarLicencia_persiste_la_solicitud_pendiente_del_docente()
	{
		var docente = await SembrarDocenteAsync(d => d.ConPuesto());

		await EnUnScope(sp => sp.GetRequiredService<IServicioLicencia>().SolicitarLicencia(
			new NuevaSolicitudLicenciaRequest(docente.Id, "Enfermedad", DateTime.Today, Dias: 0, "gripe")));

		// Scope nuevo: la consulta relee la licencia con otra unidad de trabajo / otro DbContext.
		var licencias = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioLicencia>().BuscarLicenciasSegunDocenteAsync(new DocenteIDRequest(docente.Id)));

		var licencia = licencias.ShouldHaveSingleItem();
		licencia.DocenteID.ShouldBe(docente.Id);
		licencia.Articulo.ShouldBe("Enfermedad");
		licencia.Estado.ShouldBe("Pendiente");
		licencia.FechaInicio.Date.ShouldBe(DateTime.Today);
		licencia.Observacion.ShouldBe("gripe");
	}

	[RequiereSqlServerFact]
	public async Task SolicitarLicencia_con_un_docente_inexistente_propaga_DocenteNoEncontrado()
	{
		await Should.ThrowAsync<DocenteNoEncontradoException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioLicencia>().SolicitarLicencia(
				new NuevaSolicitudLicenciaRequest(Guid.NewGuid(), "Enfermedad", DateTime.Today, Dias: 0, "gripe"))));
	}

	/// <remarks>
	/// Recorre el despacho real: <c>AprobarLicencia</c> encola <c>LicenciaActivadaEvent</c>,
	/// <c>GuardarCambiosAsync</c> lo publica antes de <c>SaveChangesAsync</c> y el handler releva
	/// al docente sobre cátedras rastreadas por el mismo contexto, así que un solo guardado
	/// persiste licencia y relevo. Con el fake de <c>Core.UnitTests</c> no se ve si ese relevo
	/// llega a la base.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task AprobarLicencia_releva_al_docente_de_las_catedras_donde_esta_en_funciones_y_conserva_la_designacion()
	{
		var docente = await SembrarDocenteAsync(d => d.ConPuesto());
		var otroDocente = await SembrarDocenteAsync(d => d.ConPuesto());
		var (propiaID, ajenaID) = await SembrarCatedrasEnFuncionesAsync(docente.Id, otroDocente.Id);

		await EnUnScope(sp => sp.GetRequiredService<IServicioLicencia>().SolicitarLicencia(
			new NuevaSolicitudLicenciaRequest(docente.Id, "Enfermedad", DateTime.Today, Dias: 0, "gripe")));
		var licencia = (await EnUnScope(sp =>
			sp.GetRequiredService<IServicioLicencia>().BuscarLicenciasSegunDocenteAsync(new DocenteIDRequest(docente.Id))))
			.ShouldHaveSingleItem();

		await EnUnScope(sp => sp.GetRequiredService<IServicioLicencia>().AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.LicenciaID, docente.Id, "Aprobada")));

		await using var contexto = Fixture.CrearContexto();
		var propia = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == propiaID);
		var ajena = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == ajenaID);

		propia.SituacionEnFuncionesID.ShouldBeNull();
		propia.SituacionesRevista.ShouldHaveSingleItem().EsCargoVigente().ShouldBeTrue();
		ajena.DocenteEnFunciones().ShouldBe(otroDocente.Id);
	}

	/// <summary>
	/// Dos cátedras: en la propia el docente está en funciones; en la ajena está designado como
	/// suplente pero en funciones está otro. El relevo no tiene que tocar la ajena.
	/// </summary>
	private async Task<(Guid PropiaID, Guid AjenaID)> SembrarCatedrasEnFuncionesAsync(Guid docenteID, Guid otroDocenteID)
	{
		var curso = await SembrarCursoAsync();
		var division = await SembrarDivisionAsync(curso.Id);

		Guid materiaID;
		Guid otraMateriaID;
		await using (var contexto = Fixture.CrearContexto())
		{
			var curricula = new CurriculaBuilder().ConCurso(curso.Id).ConFechaInicio(DateTime.Today.AddDays(-30)).Build();
			contexto.Add(curricula);
			await contexto.SaveChangesAsync();

			var materia = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Matemática").ConHorasCatedra(4).Build();
			var otraMateria = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Historia").ConHorasCatedra(4).Build();
			contexto.AddRange(materia, otraMateria);
			await contexto.SaveChangesAsync();

			materiaID = materia.Id;
			otraMateriaID = otraMateria.Id;
		}

		Guid propiaID;
		Guid ajenaID;
		Guid situacionPropia;
		Guid situacionAjena;
		await using (var contexto = Fixture.CrearContexto())
		{
			var propia = new CatedraBuilder().ConMateria(materiaID).ConDivision(division.Id).Build();
			situacionPropia = propia.Designar(docenteID, Cargo.Titular, DateTime.Today.AddDays(-10), null);

			var ajena = new CatedraBuilder().ConMateria(otraMateriaID).ConDivision(division.Id).Build();
			situacionAjena = ajena.Designar(otroDocenteID, Cargo.Titular, DateTime.Today.AddDays(-10), null);
			ajena.Designar(docenteID, Cargo.Suplente, DateTime.Today.AddDays(-5), DateTime.Today.AddMonths(1), situacionAjena);

			contexto.AddRange(propia, ajena);
			await contexto.SaveChangesAsync();

			propiaID = propia.Id;
			ajenaID = ajena.Id;
		}

		// Poner en funciones va en un SaveChanges aparte: insertar cátedra y situación en
		// funciones juntas choca con el ciclo FK_CATEDRA_SITUACION_REVISTA ↔ FK_SITUACION_REVISTA_CATEDRA.
		await using (var contexto = Fixture.CrearContexto())
		{
			var propia = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == propiaID);
			var ajena = await contexto.Catedras.Include(x => x.SituacionesRevista).SingleAsync(x => x.Id == ajenaID);
			propia.PonerEnFunciones(situacionPropia);
			ajena.PonerEnFunciones(situacionAjena);
			await contexto.SaveChangesAsync();
		}

		return (propiaID, ajenaID);
	}
}
