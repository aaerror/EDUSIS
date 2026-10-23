using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Shared.Messages;
using WPF_Desktop.ViewModels.Shared.Modal;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre el buscador de docentes del modal: a qué método del servicio llama según el término, que el
/// resultado vuelve por el mensajero, y que se cierra solo vaciando el store del modal.
/// </summary>
public class BuscarViewModelTests
{
	private sealed class Contexto
	{
		public IServicioDocente ServicioDocente { get; } = Substitute.For<IServicioDocente>();
		public DialogServiceFalso Dialogos { get; } = new();
		public ModalWindowNavigationStore ModalStore { get; } = new();
		public IMessenger Mensajero { get; } = new WeakReferenceMessenger();
		public BuscarViewModel ViewModel { get; }

		public Contexto()
		{
			DevolverDeLaNomina();
			DevolverDeLaBusqueda();

			ViewModel = new BuscarViewModel(ServicioDocente, Dialogos, ModalStore, Mensajero);

			// El modal está abierto: el store lo tiene como contenido, igual que en la app.
			ModalStore.ViewModel = ViewModel;
		}

		public void DevolverDeLaNomina(params LegajoDocenteResponse[] docentes)
		{
			ServicioDocente.ListarDocentesActivosAsync()
						   .Returns(Task.FromResult<IReadOnlyCollection<LegajoDocenteResponse>>(docentes.ToList()));
		}

		public void DevolverDeLaBusqueda(params LegajoDocenteResponse[] docentes)
		{
			ServicioDocente.BuscarDocenteSegunNombreCompletoAsync(Arg.Any<NombreCompletoRequest>())
						   .Returns(Task.FromResult<IReadOnlyCollection<LegajoDocenteResponse>>(docentes.ToList()));
		}
	}

	private static LegajoDocenteResponse CrearDocente(string nombre = "Pérez, María", Guid? docenteID = null)
	{
		return new LegajoDocenteResponse(docenteID ?? Guid.NewGuid(),
										 nombre,
										 "30111222",
										 "20301112223",
										 "L-1",
										 DateTime.Today.AddYears(-3),
										 null,
										 true);
	}

	#region BuscarCommandAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_termino_lista_la_nomina_de_docentes_activos()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaNomina(CrearDocente("Gómez, Ana"), CrearDocente("Luna, Juan"));

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Resultados.Count.ShouldBe(2);
		await contexto.ServicioDocente.Received(1).ListarDocentesActivosAsync();
		await contexto.ServicioDocente.DidNotReceive().BuscarDocenteSegunNombreCompletoAsync(Arg.Any<NombreCompletoRequest>());
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("   ")]
	[InlineData("")]
	public async Task Un_termino_en_blanco_cuenta_como_sin_termino(string termino)
	{
		var contexto = new Contexto();
		contexto.ViewModel.Termino = termino;

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		await contexto.ServicioDocente.Received(1).ListarDocentesActivosAsync();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Con_termino_busca_por_nombre_completo_y_sin_los_espacios_de_los_bordes()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaBusqueda(CrearDocente());
		contexto.ViewModel.Termino = "  Pérez  ";

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Resultados.Count.ShouldBe(1);
		await contexto.ServicioDocente.Received(1)
					  .BuscarDocenteSegunNombreCompletoAsync(Arg.Is<NombreCompletoRequest>(r => r.NombreCompleto == "Pérez"));
		await contexto.ServicioDocente.DidNotReceive().ListarDocentesActivosAsync();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_busqueda_nueva_descarta_la_seleccion_anterior()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaBusqueda(CrearDocente());
		contexto.ViewModel.Termino = "Pérez";
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionado = contexto.ViewModel.Resultados[0];

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Seleccionado.ShouldBeNull();
		contexto.ViewModel.ElegirCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_busqueda_sin_coincidencias_no_abre_ningun_cuadro_de_mensaje()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaBusqueda();

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Resultados.ShouldBeEmpty();
		contexto.Dialogos.Informaciones.ShouldBeEmpty();
		contexto.Dialogos.Advertencias.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_se_avisa_y_el_modal_queda_abierto()
	{
		var contexto = new Contexto();
		contexto.ServicioDocente.ListarDocentesActivosAsync()
				.Returns<Task<IReadOnlyCollection<LegajoDocenteResponse>>>(_ => throw new InvalidOperationException("se cayó la base"));

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Errores.ShouldHaveSingleItem().Mensaje.ShouldBe("se cayó la base");
		contexto.ModalStore.EstaAbierto.ShouldBeTrue();
	}
	#endregion

	#region ElegirCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_docente_seleccionado_no_se_puede_elegir()
	{
		var contexto = new Contexto();

		contexto.ViewModel.ElegirCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_manda_el_docente_por_el_mensajero_y_cierra_el_modal()
	{
		var contexto = new Contexto();
		var docenteID = Guid.NewGuid();
		contexto.DevolverDeLaBusqueda(CrearDocente("Luna, Juan", docenteID));
		contexto.ViewModel.Termino = "Luna";
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionado = contexto.ViewModel.Resultados[0];

		DocenteElegidoMessage recibido = null;
		contexto.Mensajero.Register<DocenteElegidoMessage>(this, (_, mensaje) => recibido = mensaje);

		contexto.ViewModel.ElegirCommand.Execute(null);

		recibido.ShouldNotBeNull();
		recibido.Docente.DocenteID.ShouldBe(docenteID);
		recibido.Docente.NombreCompleto.ShouldBe("Luna, Juan");
		contexto.ModalStore.EstaAbierto.ShouldBeFalse();
	}
	#endregion

	#region CancelarCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cancelar_cierra_el_modal_sin_avisarle_a_nadie()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaBusqueda(CrearDocente());
		contexto.ViewModel.Termino = "Pérez";
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionado = contexto.ViewModel.Resultados[0];

		var huboMensaje = false;
		contexto.Mensajero.Register<DocenteElegidoMessage>(this, (_, _) => huboMensaje = true);

		contexto.ViewModel.CancelarCommand.Execute(null);

		huboMensaje.ShouldBeFalse();
		contexto.ModalStore.EstaAbierto.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Al_cerrarse_deja_el_buscador_limpio_para_la_proxima_vez()
	{
		var contexto = new Contexto();
		contexto.DevolverDeLaBusqueda(CrearDocente());
		contexto.ViewModel.Termino = "Pérez";
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Seleccionado = contexto.ViewModel.Resultados[0];

		contexto.ViewModel.CancelarCommand.Execute(null);

		contexto.ViewModel.Termino.ShouldBeEmpty();
		contexto.ViewModel.Resultados.ShouldBeEmpty();
		contexto.ViewModel.Seleccionado.ShouldBeNull();
	}
	#endregion
}
