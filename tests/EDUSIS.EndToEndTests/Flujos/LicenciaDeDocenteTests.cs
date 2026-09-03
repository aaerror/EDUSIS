using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 3 (SC-003 / data-model.md §6): solicitud de licencia de un docente desde la fachada
/// <see cref="IServicioLicencia.SolicitarLicencia"/> hasta SQL Server real, con la composición de
/// dependencias de producción.
///
/// <para>
/// El <strong>efecto</strong> del evento de licencia sobre el curso/cursante del docente depende
/// de que <c>LicenciaSolicitadaEventHandler</c> (assembly <c>Core</c>,
/// <c>Core/ServicioCursos/Events/</c>) esté registrado en la composición real.
/// <c>InfrastructureDI</c> sólo escanea el assembly de <c>Infrastructure</c>
/// (<c>RegisterServicesFromAssemblyContaining&lt;EdusisDBContext&gt;()</c>), así que ese handler
/// <strong>no</strong> queda registrado: es el defecto <c>H-022</c> de <c>hallazgos.md</c>
/// (verificado en <c>Infrastructure.IntegrationTests.Eventos.DespachoDeEventosTests</c>). La
/// prueba del efecto queda <c>Skip</c> y no cuenta como fallo de SC-003 (FR-018, tasks.md
/// T051/T056). Acá se verifica que la solicitud llega a SQL Server y queda pendiente.
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
	public async Task SolicitarLicencia_con_un_docente_inexistente_propaga_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioLicencia>().SolicitarLicencia(
				new NuevaSolicitudLicenciaRequest(Guid.NewGuid(), "Enfermedad", DateTime.Today, Dias: 0, "gripe"))));
	}

	[Fact(Skip = "H-022: InfrastructureDI no escanea el assembly Core; LicenciaSolicitadaEventHandler no se registra y SolicitarLicencia publica el evento al vacío. El efecto sobre el curso/cursante del docente no es observable con la composición real. Ver hallazgos.md.")]
	public void SolicitarLicencia_aplica_el_efecto_del_evento_sobre_el_curso_del_docente()
	{
	}
}
