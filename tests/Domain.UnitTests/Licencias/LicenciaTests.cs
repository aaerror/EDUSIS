using Domain.Licencias;
using Domain.Licencias.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Licencias;

/// <summary>
/// Agregado <see cref="Licencia"/> con los enumerados <see cref="Articulo"/> y <see cref="Estado"/>:
/// solicitud, aprobación, cancelación y finalización, cada transición con su evento de dominio.
/// Las excepciones del módulo son <c>internal</c>: se verifican por el nombre del tipo.
/// </summary>
/// <remarks>
/// Ojo: <c>Licencia.EsIndefinida()</c> devuelve <c>true</c> cuando el período <b>sí</b> tiene
/// fecha de fin (está invertido respecto del nombre), y así lo consumen los guards.
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class LicenciaTests
{
	#region Solicitud
	[Fact]
	public void Una_licencia_nueva_arranca_pendiente_y_encola_LicenciaSolicitadaEvent()
	{
		var docenteID = Guid.NewGuid();

		var licencia = new LicenciaBuilder().ConDocente(docenteID).ConArticulo(nameof(Articulo.Enfermedad)).Build();

		licencia.DocenteID.ShouldBe(docenteID);
		licencia.Articulo.ShouldBe(Articulo.Enfermedad);
		licencia.Estado.ShouldBe(Estado.Pendiente);
		var evento = licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaSolicitadaEvent>();
		evento.LicenciaID.ShouldBe(licencia.Id);
		evento.DocenteID.ShouldBe(docenteID);
	}

	[Fact]
	public void Una_licencia_con_fecha_de_inicio_futura_lanza_ArgumentException()
	{
		var builder = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(1));

		Should.Throw<ArgumentException>(() => builder.Build());
	}
	#endregion

	#region Transiciones de estado
	[Fact]
	public void AprobarLicencia_la_pone_activa_y_encola_LicenciaActivadaEvent()
	{
		var licencia = new LicenciaBuilder().Build();
		licencia.LiberarEventos();

		licencia.AprobarLicencia("Aprobada por dirección");

		licencia.Estado.ShouldBe(Estado.Activa);
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaActivadaEvent>();
	}

	[Fact]
	public void CancelarLicencia_la_pone_cancelada_y_encola_LicenciaCanceladaEvent()
	{
		var licencia = new LicenciaBuilder().Build();
		licencia.LiberarEventos();

		licencia.CancelarLicencia("Rechazada");

		licencia.Estado.ShouldBe(Estado.Cancelada);
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaCanceladaEvent>();
	}

	[Fact]
	public void Aprobar_o_cancelar_una_licencia_que_no_esta_pendiente_lanza_ArgumentException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		Should.Throw<ArgumentException>(() => licencia.AprobarLicencia(null));
		Should.Throw<ArgumentException>(() => licencia.CancelarLicencia(null));
	}

	[Fact]
	public void FinalizarLicencia_sobre_una_licencia_activa_la_finaliza_y_encola_LicenciaFinalizadaEvent()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();
		licencia.LiberarEventos();

		licencia.FinalizarLicencia();

		licencia.Estado.ShouldBe(Estado.Finalizada);
		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today);
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaFinalizadaEvent>();
	}

	[Fact]
	public void FinalizarLicencia_sobre_una_licencia_pendiente_lanza_LicenciaInactivaException()
	{
		var licencia = new LicenciaBuilder().Build();

		var ex = Should.Throw<Exception>(() => licencia.FinalizarLicencia());

		ex.GetType().Name.ShouldBe("LicenciaInactivaException");
	}
	#endregion

	#region Modificación del período
	[Fact]
	public void ModificarPeriodo_sobre_una_licencia_no_activa_ajusta_el_intervalo()
	{
		var licencia = new LicenciaBuilder().Build();

		licencia.ModificarPeriodo(new DateTime(2026, 5, 1), 10);

		licencia.Periodo.FechaInicio.ShouldBe(new DateTime(2026, 5, 1));
		licencia.Periodo.FechaFin.ShouldBe(new DateTime(2026, 5, 11));
	}

	[Fact]
	public void ModificarPeriodo_sobre_una_licencia_activa_lanza_LicenciaActivaException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.ModificarPeriodo(DateTime.Today, 5));

		ex.GetType().Name.ShouldBe("LicenciaActivaException");
	}

	[Fact]
	public void EstablecerLicenciaIndefinida_sobre_una_licencia_activa_con_fecha_de_fin_lanza_LicenciaIndefinidaException()
	{
		var licencia = new LicenciaBuilder().ConFechaFin(DateTime.Today.AddDays(20)).Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.EstablecerLicenciaIndefinida());

		ex.GetType().Name.ShouldBe("LicenciaIndefinidaException");
	}

	[Fact]
	public void EstablecerFechaFinalizacion_con_una_fecha_no_posterior_a_hoy_lanza_ArgumentException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		Should.Throw<ArgumentException>(() => licencia.EstablecerFechaFinalizacion(DateTime.Today));
	}

	[Fact(Skip = "H-013: Licencia.ExtenderLicencia nunca funciona: si el período es temporal EsIndefinida() es true y lanza LicenciaIndefinidaException; si es indeterminado, accede a Periodo.FechaFin.Value con FechaFin null. Ver hallazgos.md.")]
	public void ExtenderLicencia_prolonga_la_fecha_de_fin_de_una_licencia_activa_temporal()
	{
	}
	#endregion
}
