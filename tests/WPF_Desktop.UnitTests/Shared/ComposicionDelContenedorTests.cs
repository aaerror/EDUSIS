using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.Store.NavigationStore;
using Xunit;

namespace WPF_Desktop.UnitTests.Shared;

/// <summary>
/// Reproduce la cadena de registro de <c>App.xaml.cs</c> y verifica que los Singleton de la capa
/// de presentación sean una sola instancia por raíz. Es la prueba que habría detectado E.2
/// (dos contenedores sobre la misma IServiceCollection) sin levantar la UI.
/// </summary>
public class ComposicionDelContenedorTests
{
	private static IHost ConstruirHost()
	{
		// Igual que App.xaml.cs: Host.CreateDefaultBuilder + Infrastructure -> Core -> WPF_Desktop.
		return Host.CreateDefaultBuilder()
			.ConfigureServices((contexto, services) =>
			{
				InfrastructureDI.AddInfrastructure(services);
				CoreDI.AddServices(services);
				WPF_DesktopDI.AddWPFDesktopDI(services);
			})
			.Build();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_contenedor_del_host_se_construye_sin_base_de_datos()
	{
		IHost? host = null;
		Exception? error = null;

		try
		{
			host = ConstruirHost();
		}
		catch (Exception ex)
		{
			error = ex;
		}

		error.ShouldBeNull();
		host.ShouldNotBeNull();
		host!.Dispose();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void MainWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<MainWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<MainWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void StartupWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<StartupWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<StartupWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ModalWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<ModalWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<ModalWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CicloLectivoStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<CicloLectivoStore>();
		var segunda = host.Services.GetRequiredService<CicloLectivoStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CatedraStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<CatedraStore>();
		var segunda = host.Services.GetRequiredService<CatedraStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_cambio_escrito_en_un_store_lo_ve_quien_lo_resuelve_despues()
	{
		using var host = ConstruirHost();
		var escritor = host.Services.GetRequiredService<CicloLectivoStore>();
		escritor.CicloLectivo = "2031";

		var lector = host.Services.GetRequiredService<CicloLectivoStore>();

		lector.CicloLectivo.ShouldBe("2031");
	}
}
