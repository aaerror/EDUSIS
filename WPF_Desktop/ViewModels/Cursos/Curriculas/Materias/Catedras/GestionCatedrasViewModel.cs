using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;

internal partial class GestionCatedrasViewModel : ObservableObject
{
	#region Services
	private readonly IServicioCatedra _servicioCatedra;
	private readonly IServicioDivision _servicioDivision;
	private readonly IDialogService _dialogService;
	private readonly INavigationService _gestionMateriasNavigationService;
	private readonly INavigationService _gestionSituacionRevistaNavigationService;
	#endregion

	#region Store
	private readonly MateriaStore _materiaStore;
	private readonly CatedraStore _catedraStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	[ObservableProperty]
	private MateriaViewModel? _materia;

	[ObservableProperty]
	private ObservableCollection<CatedraViewModel> _catedras = new();

	[ObservableProperty]
	private ObservableCollection<DivisionViewModel> _divisionesSinCatedra = new();

	[NotifyCanExecuteChangedFor(nameof(CrearCatedraCommand))]
	[ObservableProperty]
	private DivisionViewModel? _divisionSeleccionada;

	#region Commands
	public IAsyncRelayCommand CargarCommand { get; }
	public IAsyncRelayCommand CrearCatedraCommand { get; }
	public IRelayCommand<CatedraViewModel> SeleccionarCatedraCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion


	/// <remarks>
	/// ORDEN DE LOS DOS <see cref="INavigationService"/> (violación #7 del plan, no corregida): ambos son del mismo tipo
	/// y la factory lambda de <c>WPF_DesktopDI</c> los distingue SÓLO por posición. Invertirlos compila y rompe la
	/// navegación en silencio. Orden obligatorio: 1) <paramref name="gestionMateriasNavigationService"/> (volver a materias),
	/// 2) <paramref name="gestionSituacionRevistaNavigationService"/> (ir a situación de revista).
	/// </remarks>
	public GestionCatedrasViewModel(INavigationService gestionMateriasNavigationService,
									INavigationService gestionSituacionRevistaNavigationService,
									IServicioCatedra servicioCatedra,
									IServicioDivision servicioDivision,
									MateriaStore materiaStore,
									CatedraStore catedraStore,
									CicloLectivoStore cicloLectivoStore,
									IDialogService dialogService)
	{
		_gestionMateriasNavigationService = gestionMateriasNavigationService;
		_gestionSituacionRevistaNavigationService = gestionSituacionRevistaNavigationService;
		_servicioCatedra = servicioCatedra;
		_servicioDivision = servicioDivision;

		_materiaStore = materiaStore;
		_catedraStore = catedraStore;
		_cicloLectivoStore = cicloLectivoStore;
		_dialogService = dialogService;

		Materia = _materiaStore.Materia;

		CargarCommand = new AsyncRelayCommand(ExecuteCargarCommand, CanExecuteCargarCommand);
		CrearCatedraCommand = new AsyncRelayCommand(ExecuteCrearCatedraCommand, CanExecuteCrearCatedraCommand);
		SeleccionarCatedraCommand = new RelayCommand<CatedraViewModel>(ExecuteSeleccionarCatedraCommand, CanExecuteSeleccionarCatedraCommand);
		VolverCommand = new RelayCommand(ExecuteVolverCommand, CanExecuteVolverCommand);
	}

	#region CargarCommand
	private bool CanExecuteCargarCommand()
	{
		return Materia is not null;
	}

	private async Task ExecuteCargarCommand()
	{
		try
		{
			await CargarCatedrasAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Cátedras");
		}
	}

	private async Task CargarCatedrasAsync()
	{
		if (Materia is null)
		{
			return;
		}

		var catedras = await _servicioCatedra.ListarCatedrasSegunMateriaAsync(new ListarCatedrasSegunMateriaRequest(MateriaID: Materia.MateriaID));
		var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(CursoID: Materia.CursoID,
																								   CicloLectivo: _cicloLectivoStore.CicloLectivo));

		Dictionary<Guid, string> letras = divisiones.ToDictionary(d => d.DivisionID, d => d.Descripcion);

		Catedras.Clear();
		foreach (var catedra in catedras)
		{
			var viewModel = new CatedraViewModel(catedra);
			viewModel.Division = letras.TryGetValue(catedra.DivisionID, out var letra) ? letra : string.Empty;
			Catedras.Add(viewModel);
		}

		HashSet<Guid> conCatedra = catedras.Select(c => c.DivisionID).ToHashSet();

		DivisionesSinCatedra.Clear();
		foreach (var division in divisiones.Where(d => !conCatedra.Contains(d.DivisionID)))
		{
			DivisionesSinCatedra.Add(new DivisionViewModel(division));
		}

		DivisionSeleccionada = null;
	}
	#endregion

	#region CrearCatedraCommand
	private bool CanExecuteCrearCatedraCommand()
	{
		return Materia is not null && DivisionSeleccionada is not null;
	}

	private async Task ExecuteCrearCatedraCommand()
	{
		try
		{
			if (Materia is null || DivisionSeleccionada is null)
			{
				return;
			}

			await _servicioCatedra.CrearCatedraAsync(new CrearCatedraRequest(MateriaID: Materia.MateriaID,
																			 DivisionID: DivisionSeleccionada.DivisionID));

			await CargarCatedrasAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Cátedras");
		}
	}
	#endregion

	#region SeleccionarCatedraCommand
	private bool CanExecuteSeleccionarCatedraCommand(CatedraViewModel catedra)
	{
		return catedra is not null && catedra.CatedraID != Guid.Empty;
	}

	private void ExecuteSeleccionarCatedraCommand(CatedraViewModel catedra)
	{
		_catedraStore.Catedra = catedra.CatedraID;
		_gestionSituacionRevistaNavigationService.Navigate();
	}
	#endregion

	#region VolverCommand
	private bool CanExecuteVolverCommand()
	{
		return true;
	}

	private void ExecuteVolverCommand()
	{
		_gestionMateriasNavigationService.Navigate();
	}
	#endregion
}
