using Domain.Licencias;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1: persistir y releer una <see cref="Licencia"/> independiente. Verifica la clave
/// compuesta (<c>licencia_id</c>, <c>docente_id</c>), las conversiones de enum
/// (<see cref="Articulo"/>, <see cref="Estado"/>) y el value object <c>Periodo</c>.
/// </summary>
public sealed class MapeoDeLicenciaTests : BaseIntegracion
{
	public MapeoDeLicenciaTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Una_licencia_temporal_se_relee_con_su_articulo_estado_y_periodo()
	{
		var docenteID = await SembrarDocenteAsync();
		var inicio = DateTime.Today;
		var fin = DateTime.Today.AddDays(10);

		var licencia = new LicenciaBuilder()
			.ConDocente(docenteID)
			.ConArticulo("Enfermedad")
			.ConFechaInicio(inicio)
			.ConFechaFin(fin)
			.ConObservacion("gripe")
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(licencia);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releida = await contexto.Set<Licencia>()
				.SingleAsync(x => x.Id == licencia.Id && x.DocenteID == docenteID);

			releida.Articulo.ShouldBe(Articulo.Enfermedad);
			releida.Estado.ShouldBe(Estado.Pendiente);
			releida.Periodo.FechaInicio.ShouldBe(inicio.Date);
			releida.Periodo.FechaFin!.Value.Date.ShouldBe(fin.Date);
			releida.Observacion.ShouldBe("gripe");
		}
	}

	/// <summary>
	/// Fija el ancho de <c>observacion</c> contra la base: una observación de longitud realista
	/// persiste y vuelve sin truncar.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Cubría el defecto H-019 —<c>LicenciasConfiguration</c> mapeaba <c>Observacion</c> como
	/// <c>varchar(10)</c> mientras el dominio (<c>Licencia.ModificarObservaciones</c>) admite 250,
	/// así que una observación real revienta con "String or binary data would be truncated"— y
	/// llevaba <c>[Fact(Skip=…)]</c> por eso.
	/// </para>
	/// <para>
	/// <strong>El Skip se retiró el 2026-11-09</strong>: <c>LicenciasConfiguration.cs:46</c>
	/// configura <c>HasMaxLength(250).IsUnicode(false)</c>, así que la causa ya no existe y la
	/// prueba pasa contra SQL Server real. Era un <c>Skip</c> que había sobrevivido a su causa —el
	/// mismo caso que H-010 y H-023—; detalle en <c>docs/todos-historial.md</c>, nota del
	/// 2026-11-09 (segunda). <c>TEST-01</c> bajó de 7 a 5 pruebas omitidas.
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task Una_observacion_de_licencia_de_longitud_realista_se_persiste_sin_truncar()
	{
		var docenteID = await SembrarDocenteAsync();
		var licencia = new LicenciaBuilder()
			.ConDocente(docenteID)
			.ConObservacion("Reposo por indicación médica durante diez días corridos.")
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(licencia);
		await contexto.SaveChangesAsync();

		await using var otro = Fixture.CrearContexto();
		var releida = await otro.Set<Licencia>().SingleAsync(x => x.Id == licencia.Id && x.DocenteID == docenteID);
		releida.Observacion.ShouldBe("Reposo por indicación médica durante diez días corridos.");
	}
}
