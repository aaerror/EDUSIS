using Core.ServicioDocentes.DTOs.Requests;
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
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Docentes.Licencias;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// <see cref="GestionLicenciasViewModel"/>: el panel de la licencia en curso (reintegro, prórroga,
/// fecha de fin, indefinida). Qué se ofrece según la licencia elegida y qué request sale.
/// </summary>
public class GestionLicenciasViewModelTests
{
	private sealed class Contexto
	{
		public IServicioLicencia Servicio { get; } = Substitute.For<IServicioLicencia>();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid DocenteID { get; } = Guid.NewGuid();
		public GestionLicenciasViewModel ViewModel { get; }

		public Contexto(params Permiso[] permisos)
		{
			Devolver();
			var legajo = new LegajoStore { PersonaID = DocenteID };
			ViewModel = new GestionLicenciasViewModel(Servicio, Dialogos, new NavegacionFalsa<GestionDocentesViewModel>(), legajo,
													  permisos.Length == 0 ? new SesionFalsa() : new SesionFalsa(permisos));
		}

		public void Devolver(params LicenciaResponse[] licencias) =>
			Servicio.BuscarLicenciasSegunDocenteAsync(Arg.Any<DocenteIDRequest>())
					.Returns(Task.FromResult<IReadOnlyCollection<LicenciaResponse>>(licencias.ToList()));

		public async Task ElegirAsync(LicenciaResponse licencia)
		{
			Devolver(licencia);
			await ViewModel.CargarLicenciasCommandAsync.ExecuteAsync(null);
			ViewModel.LicenciaSeleccionada = ViewModel.Licencias.Single();
		}
	}

	private static LicenciaResponse Licencia(string estado, DateTime? fin, Articulo articulo = Articulo.AccidenteDeTrabajo, bool vencida = false) =>
		new(Guid.NewGuid(), Guid.NewGuid(), articulo, estado, fin.HasValue ? 5 : 0, DateTime.Today.AddDays(-4), fin, "obs", vencida);

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_pendiente_no_muestra_el_panel_de_licencia_en_curso()
	{
		var contexto = new Contexto();

		await contexto.ElegirAsync(Licencia(nameof(Estado.Pendiente), DateTime.Today));

		contexto.ViewModel.MostrarLicenciaEnCurso.ShouldBeFalse();
		contexto.ViewModel.GuardarCommandAsync.CanExecute("Finalizar").ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_aprobada_con_fin_ofrece_reintegro_y_prorroga_pero_no_fijar_fin()
	{
		var contexto = new Contexto();

		await contexto.ElegirAsync(Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3)));

		contexto.ViewModel.MostrarLicenciaEnCurso.ShouldBeTrue();
		contexto.ViewModel.PuedeProrrogar.ShouldBeTrue();
		contexto.ViewModel.PuedeFijarFechaDeFin.ShouldBeFalse();
		contexto.ViewModel.GuardarCommandAsync.CanExecute("Finalizar").ShouldBeTrue();
		contexto.ViewModel.GuardarCommandAsync.CanExecute("Prorrogar").ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_aprobada_indefinida_ofrece_fijar_fin_solo_con_una_fecha_posterior_a_hoy()
	{
		var contexto = new Contexto();
		await contexto.ElegirAsync(Licencia(nameof(Estado.Activa), null));

		contexto.ViewModel.PuedeFijarFechaDeFin.ShouldBeTrue();
		contexto.ViewModel.PuedeProrrogar.ShouldBeFalse();
		contexto.ViewModel.GuardarCommandAsync.CanExecute("FijarFin").ShouldBeFalse();

		contexto.ViewModel.FechaDeFin = DateTime.Today;
		contexto.ViewModel.GuardarCommandAsync.CanExecute("FijarFin").ShouldBeFalse();

		contexto.ViewModel.FechaDeFin = DateTime.Today.AddDays(1);
		contexto.ViewModel.GuardarCommandAsync.CanExecute("FijarFin").ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Declarar_indefinida_se_ofrece_solo_en_un_articulo_de_plazo_abierto()
	{
		var abierto = new Contexto();
		await abierto.ElegirAsync(Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3), Articulo.AccidenteDeTrabajo));
		var conPlazo = new Contexto();
		await conPlazo.ElegirAsync(Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3), Articulo.EnfermedadCortoTratamiento));

		abierto.ViewModel.PuedeDeclararIndefinida.ShouldBeTrue();
		conPlazo.ViewModel.PuedeDeclararIndefinida.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_el_permiso_del_articulo_no_se_muestra_el_panel()
	{
		var contexto = new Contexto(Permiso.GestionarDocentes, Permiso.ResolverLicencias);

		await contexto.ElegirAsync(Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3), Articulo.OtrasCausas));

		contexto.ViewModel.MostrarLicenciaEnCurso.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_vencida_se_marca_y_admite_el_reintegro()
	{
		var contexto = new Contexto();

		await contexto.ElegirAsync(Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(-1), vencida: true));

		contexto.ViewModel.LicenciaSeleccionada.EtiquetaEstado.ShouldBe("Vencida");
		contexto.ViewModel.GuardarCommandAsync.CanExecute("Finalizar").ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Registrar_el_reintegro_finaliza_la_licencia_del_docente()
	{
		var contexto = new Contexto();
		var licencia = Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3));
		await contexto.ElegirAsync(licencia);

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Finalizar");

		await contexto.Servicio.Received(1).FinalizarLicencia(new FinalizarLicenciaRequest(licencia.LicenciaID, contexto.DocenteID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Prorrogar_manda_los_dias_escritos()
	{
		var contexto = new Contexto();
		var licencia = Licencia(nameof(Estado.Activa), DateTime.Today.AddDays(3));
		await contexto.ElegirAsync(licencia);
		contexto.ViewModel.DiasDeProrroga = 7;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Prorrogar");

		await contexto.Servicio.Received(1).ExtenderLicencia(new ExtenderLicenciaRequest(licencia.LicenciaID, contexto.DocenteID, 7));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Rechazar_una_pendiente_usa_el_parametro_Rechazar()
	{
		var contexto = new Contexto();
		await contexto.ElegirAsync(Licencia(nameof(Estado.Pendiente), DateTime.Today));

		contexto.ViewModel.GuardarCommandAsync.CanExecute("Rechazar").ShouldBeTrue();
		contexto.ViewModel.GuardarCommandAsync.CanExecute("Cancelar").ShouldBeFalse();
	}
}
