using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Responses;
using Domain.Licencias;
using Domain.Usuarios;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Licencias;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// <see cref="LicenciasPendientesViewModel"/>: la bandeja de pendientes. Cubre la carga, que los
/// botones sigan el permiso del artículo de la fila y que la decisión lleve la observación.
/// </summary>
public class LicenciasPendientesViewModelTests
{
	private sealed class Contexto
	{
		public IServicioLicencia Servicio { get; } = Substitute.For<IServicioLicencia>();
		public DialogServiceFalso Dialogos { get; } = new();
		public LicenciasPendientesViewModel ViewModel { get; }

		public Contexto(params Permiso[] permisos)
		{
			Devolver();
			ViewModel = new LicenciasPendientesViewModel(Servicio, Dialogos, permisos.Length == 0 ? new SesionFalsa() : new SesionFalsa(permisos));
		}

		public void Devolver(params LicenciaPendienteResponse[] pendientes) =>
			Servicio.ListarLicenciasPendientesAsync()
					.Returns(Task.FromResult<IReadOnlyCollection<LicenciaPendienteResponse>>(pendientes.ToList()));
	}

	private static LicenciaPendienteResponse CrearPendiente(Articulo articulo = Articulo.AccidenteDeTrabajo, string? observacion = "Certificado presentado") =>
		new(Guid.NewGuid(), Guid.NewGuid(), "Soto, Ana", articulo, 3, DateTime.Today, DateTime.Today.AddDays(2), observacion);

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_sin_pendientes_muestra_la_bandeja_vacia()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.BandejaVacia.ShouldBeTrue();
		contexto.ViewModel.Pendientes.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_trae_las_pendientes_con_la_denominacion_del_articulo()
	{
		var contexto = new Contexto();
		contexto.Devolver(CrearPendiente(Articulo.MatrimonioPropio));

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.BandejaVacia.ShouldBeFalse();
		contexto.ViewModel.Pendientes.ShouldHaveSingleItem().Denominacion.ShouldBe(PlazoLegal.De(Articulo.MatrimonioPropio).Denominacion);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_fila_elegida_no_se_aprueba_ni_se_rechaza()
	{
		var contexto = new Contexto();
		contexto.Devolver(CrearPendiente());
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.AprobarCommandAsync.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.RechazarCommandAsync.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Con_ResolverLicencias_una_academica_no_se_puede_decidir()
	{
		var contexto = new Contexto(Permiso.ResolverLicencias);
		contexto.Devolver(CrearPendiente(Articulo.Examen));
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Seleccionada = contexto.ViewModel.Pendientes.Single();

		contexto.ViewModel.AprobarCommandAsync.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.RechazarCommandAsync.CanExecute(null).ShouldBeFalse();
	}

	/// <remarks>
	/// El servicio reemplaza la observación con la que llega: elegir la fila la precarga, así una
	/// aprobación sin tocar el campo no borra lo que se cargó con la solicitud.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Aprobar_manda_la_observacion_precargada_y_recarga()
	{
		var contexto = new Contexto();
		var pendiente = CrearPendiente(observacion: "Certificado presentado");
		contexto.Devolver(pendiente);
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionada = contexto.ViewModel.Pendientes.Single();

		await contexto.ViewModel.AprobarCommandAsync.ExecuteAsync(null);

		await contexto.Servicio.Received(1).AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(pendiente.LicenciaID, pendiente.DocenteID, "Certificado presentado"));
		await contexto.Servicio.Received(2).ListarLicenciasPendientesAsync();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Rechazar_manda_la_observacion_escrita_como_motivo()
	{
		var contexto = new Contexto();
		var pendiente = CrearPendiente();
		contexto.Devolver(pendiente);
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionada = contexto.ViewModel.Pendientes.Single();
		contexto.ViewModel.Observacion = "Sin certificado médico";

		await contexto.ViewModel.RechazarCommandAsync.ExecuteAsync(null);

		await contexto.Servicio.Received(1).RechazarLicencia(
			new ActualizarEstadoLicenciaRequest(pendiente.LicenciaID, pendiente.DocenteID, "Sin certificado médico"));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_confirmar_no_se_decide()
	{
		var contexto = new Contexto();
		contexto.Devolver(CrearPendiente());
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionada = contexto.ViewModel.Pendientes.Single();
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.AprobarCommandAsync.ExecuteAsync(null);

		await contexto.Servicio.DidNotReceiveWithAnyArgs().AprobarLicencia(default!);
	}
}
