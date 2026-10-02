using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioMaterias.DTOs.Responses;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre la pantalla nueva <see cref="GestionCatedrasViewModel"/>: una sola consulta de divisiones por carga,
/// <c>CrearCatedraAsync(MateriaID, DivisionID)</c>, el <see cref="Guid"/> de la catedra en <see cref="CatedraStore"/>
/// antes de navegar, el orden de los dos <c>INavigationService</c> y que no se exponga ningun DTO de Core.
/// </summary>
public class GestionCatedrasViewModelTests
{
	private const string CicloDelStore = "1999";

	private sealed class Contexto
	{
		public IServicioCatedra ServicioCatedra { get; } = Substitute.For<IServicioCatedra>();
		public IServicioDivision ServicioDivision { get; } = Substitute.For<IServicioDivision>();
		public NavegacionFalsa NavegarAMaterias { get; } = new();
		public NavegacionFalsa NavegarASituacionRevista { get; } = new();
		public MateriaStore MateriaStore { get; } = new();
		public CatedraStore CatedraStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public Guid MateriaID { get; } = Guid.NewGuid();
		public GestionCatedrasViewModel ViewModel { get; }

		public Contexto(bool conMateria = true)
		{
			CicloLectivoStore.CicloLectivo = CicloDelStore;

			if (conMateria)
			{
				MateriaStore.Materia = new MateriaViewModel(new MateriaResponse(CursoID, Guid.NewGuid(), MateriaID, "Matematica", 4));
			}

			Devolver(new List<CatedraResponse>(), new List<DivisionResponse>());

			ViewModel = new GestionCatedrasViewModel(NavegarAMaterias,
													 NavegarASituacionRevista,
													 ServicioCatedra,
													 ServicioDivision,
													 MateriaStore,
													 CatedraStore,
													 CicloLectivoStore,
													 Dialogos);
		}

		public void Devolver(List<CatedraResponse> catedras, List<DivisionResponse> divisiones)
		{
			ServicioCatedra.ListarCatedrasSegunMateriaAsync(Arg.Any<ListarCatedrasSegunMateriaRequest>())
						   .Returns(Task.FromResult<IReadOnlyCollection<CatedraResponse>>(catedras));
			ServicioDivision.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(divisiones));
		}
	}

	private static DivisionResponse CrearDivision(string letra)
	{
		return new DivisionResponse(Guid.NewGuid(), letra, null, null, 0);
	}

	private static CatedraResponse CrearCatedra(Guid materiaID, Guid divisionID)
	{
		return new CatedraResponse(Guid.NewGuid(), materiaID, divisionID, 4, 1, 3, null);
	}

	#region Carga
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_catedras_de_la_materia_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(1).ListarCatedrasSegunMateriaAsync(new ListarCatedrasSegunMateriaRequest(contexto.MateriaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_llama_a_ListarDivisionesAsync_una_sola_vez_aunque_haya_varias_catedras()
	{
		var contexto = new Contexto();
		var divisiones = new List<DivisionResponse> { CrearDivision("A"), CrearDivision("B"), CrearDivision("C") };
		var catedras = divisiones.Select(d => CrearCatedra(contexto.MateriaID, d.DivisionID)).ToList();
		contexto.Devolver(catedras, divisiones);

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Catedras.Count.ShouldBe(3);
		_ = contexto.ServicioDivision.Received(1).ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_divisiones_con_el_curso_de_la_materia_y_el_ciclo_lectivo_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioDivision.Received(1).ListarDivisionesAsync(new ListarDivisionesRequest(contexto.CursoID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_resuelve_la_letra_de_cada_catedra_y_deja_vacia_la_de_una_division_desconocida()
	{
		var contexto = new Contexto();
		var divisionA = CrearDivision("A");
		var divisionB = CrearDivision("B");
		var catedraA = CrearCatedra(contexto.MateriaID, divisionA.DivisionID);
		var catedraB = CrearCatedra(contexto.MateriaID, divisionB.DivisionID);
		var catedraHuerfana = CrearCatedra(contexto.MateriaID, Guid.NewGuid());
		contexto.Devolver(new List<CatedraResponse> { catedraA, catedraB, catedraHuerfana },
						  new List<DivisionResponse> { divisionA, divisionB });

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Catedras.Single(c => c.CatedraID == catedraA.CatedraID).Division.ShouldBe("A");
		contexto.ViewModel.Catedras.Single(c => c.CatedraID == catedraB.CatedraID).Division.ShouldBe("B");
		contexto.ViewModel.Catedras.Single(c => c.CatedraID == catedraHuerfana.CatedraID).Division.ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_ofrece_para_crear_solo_las_divisiones_que_todavia_no_tienen_catedra_de_la_materia()
	{
		var contexto = new Contexto();
		var conCatedra = CrearDivision("A");
		var sinCatedra = CrearDivision("B");
		contexto.Devolver(new List<CatedraResponse> { CrearCatedra(contexto.MateriaID, conCatedra.DivisionID) },
						  new List<DivisionResponse> { conCatedra, sinCatedra });

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.DivisionesSinCatedra.Count.ShouldBe(1);
		contexto.ViewModel.DivisionesSinCatedra[0].DivisionID.ShouldBe(sinCatedra.DivisionID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_dos_veces_no_duplica_las_catedras()
	{
		var contexto = new Contexto();
		var division = CrearDivision("A");
		contexto.Devolver(new List<CatedraResponse> { CrearCatedra(contexto.MateriaID, division.DivisionID) },
						  new List<DivisionResponse> { division });

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Catedras.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_materia_en_el_store_la_carga_no_esta_habilitada()
	{
		var contexto = new Contexto(conMateria: false);

		contexto.ViewModel.CargarCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_cargar_se_muestra_por_el_dialogo_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioCatedra.ListarCatedrasSegunMateriaAsync(Arg.Any<ListarCatedrasSegunMateriaRequest>())
								.Returns(Task.FromException<IReadOnlyCollection<CatedraResponse>>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("sin conexion");
	}
	#endregion

	#region CrearCatedraAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_una_catedra_llama_a_CrearCatedraAsync_con_la_materia_y_la_division_elegida()
	{
		var contexto = new Contexto();
		var division = CrearDivision("B");
		contexto.Devolver(new List<CatedraResponse>(), new List<DivisionResponse> { CrearDivision("A"), division });
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.DivisionSeleccionada = contexto.ViewModel.DivisionesSinCatedra.Single(d => d.DivisionID == division.DivisionID);

		await contexto.ViewModel.CrearCatedraCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(1).CrearCatedraAsync(new CrearCatedraRequest(contexto.MateriaID, division.DivisionID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_una_catedra_recarga_la_lista_despues_de_crear()
	{
		var contexto = new Contexto();
		var division = CrearDivision("A");
		contexto.Devolver(new List<CatedraResponse>(), new List<DivisionResponse> { division });
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.DivisionSeleccionada = contexto.ViewModel.DivisionesSinCatedra[0];

		await contexto.ViewModel.CrearCatedraCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(2).ListarCatedrasSegunMateriaAsync(Arg.Any<ListarCatedrasSegunMateriaRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_una_catedra_sin_division_elegida_no_esta_habilitado_ni_llega_al_servicio()
	{
		var contexto = new Contexto();
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.CrearCatedraCommand.CanExecute(null).ShouldBeFalse();

		await contexto.ViewModel.CrearCatedraCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.DidNotReceive().CrearCatedraAsync(Arg.Any<CrearCatedraRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_una_division_habilita_el_comando_de_crear()
	{
		var contexto = new Contexto();
		contexto.Devolver(new List<CatedraResponse>(), new List<DivisionResponse> { CrearDivision("A") });
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		var cambios = 0;
		contexto.ViewModel.CrearCatedraCommand.CanExecuteChanged += (_, _) => cambios++;

		contexto.ViewModel.DivisionSeleccionada = contexto.ViewModel.DivisionesSinCatedra[0];

		cambios.ShouldBeGreaterThan(0);
		contexto.ViewModel.CrearCatedraCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_crear_se_muestra_por_el_dialogo_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.Devolver(new List<CatedraResponse>(), new List<DivisionResponse> { CrearDivision("A") });
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.DivisionSeleccionada = contexto.ViewModel.DivisionesSinCatedra[0];
		contexto.ServicioCatedra.CrearCatedraAsync(Arg.Any<CrearCatedraRequest>())
								.Returns(Task.FromException<Guid>(new InvalidOperationException("ya existe")));

		await contexto.ViewModel.CrearCatedraCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("ya existe");
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Seleccionar_una_catedra_deja_su_Guid_en_el_CatedraStore_antes_de_llamar_a_Navigate()
	{
		var contexto = new Contexto();
		var catedra = new CatedraViewModel(CrearCatedra(contexto.MateriaID, Guid.NewGuid()));
		Guid catedraAlNavegar = Guid.Empty;
		contexto.NavegarASituacionRevista.AlNavegar = () => catedraAlNavegar = contexto.CatedraStore.Catedra;

		contexto.ViewModel.SeleccionarCatedraCommand.Execute(catedra);

		contexto.NavegarASituacionRevista.Llamadas.ShouldBe(1);
		catedraAlNavegar.ShouldBe(catedra.CatedraID);
		contexto.CatedraStore.Catedra.ShouldBe(catedra.CatedraID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Seleccionar_una_catedra_navega_a_situacion_de_revista_y_no_a_materias()
	{
		// Violacion #7 del plan: los dos INavigationService se distinguen solo por su orden en el constructor.
		var contexto = new Contexto();
		var catedra = new CatedraViewModel(CrearCatedra(contexto.MateriaID, Guid.NewGuid()));

		contexto.ViewModel.SeleccionarCatedraCommand.Execute(catedra);

		contexto.NavegarASituacionRevista.Llamadas.ShouldBe(1);
		contexto.NavegarAMaterias.Llamadas.ShouldBe(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Seleccionar_una_catedra_sin_identidad_no_esta_habilitado()
	{
		var contexto = new Contexto();
		var sinIdentidad = new CatedraViewModel(null!);

		contexto.ViewModel.SeleccionarCatedraCommand.CanExecute(sinIdentidad).ShouldBeFalse();
		contexto.ViewModel.SeleccionarCatedraCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Volver_navega_a_materias_y_no_a_situacion_de_revista_ni_toca_el_CatedraStore()
	{
		var contexto = new Contexto();

		contexto.ViewModel.VolverCommand.Execute(null);

		contexto.NavegarAMaterias.Llamadas.ShouldBe(1);
		contexto.NavegarASituacionRevista.Llamadas.ShouldBe(0);
		contexto.CatedraStore.Catedra.ShouldBe(Guid.Empty);
	}
	#endregion

	#region Contrato del ViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ViewModel_no_expone_ningun_tipo_de_Core_como_propiedad_bindeable()
	{
		// Violacion #5 del plan: ObservableCollection<XxxResponse> / XxxResponse como propiedad publica.
		var propiedades = typeof(GestionCatedrasViewModel).GetProperties();

		foreach (var propiedad in propiedades)
		{
			var tipos = new[] { propiedad.PropertyType }.Concat(propiedad.PropertyType.GetGenericArguments());

			foreach (var tipo in tipos)
			{
				(tipo.Namespace ?? string.Empty).StartsWith("Core.").ShouldBeFalse($"{propiedad.Name} expone el tipo de Core {tipo.FullName}");
			}
		}
	}
	#endregion
}
