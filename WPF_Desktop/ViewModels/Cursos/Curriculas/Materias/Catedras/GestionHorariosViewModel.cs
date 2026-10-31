using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

// Los dos enums del dominio se traen con alias porque el ViewModel tiene propiedades string
// homónimas —Turno y Dia, lo que el usuario eligió en cada ComboBox—: sin alias, "Dia.Lunes"
// resuelve a la propiedad y no compila.
using DiaDelDominio = Domain.Catedras.Horarios.Dia;
using TurnoDelDominio = Domain.Catedras.Horarios.Turno;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;

/// <summary>
/// Horarios de una cátedra: la grilla de los que ya están cargados y el formulario para agregar uno nuevo.
/// Pantalla de segundo nivel, drill-down desde Cátedras: la cátedra llega por <see cref="CatedraStore"/> y la
/// descripción de la materia, que es el título, por <see cref="MateriaStore"/>.
/// </summary>
/// <remarks>
/// MODIFICAR UN HORARIO ES QUITAR + AGREGAR, por decisión explícita del plan: el dominio está modelado con
/// operaciones de ciclo de vida (agregar/quitar) y <c>Core</c> NO tiene una operación atómica de corrección.
/// Lo único que hace falta para que el recorrido sea cómodo es el hook <see cref="OnHorarioChanged"/>, que copia
/// la fila elegida al formulario: el usuario la corrige ahí y confirma los dos pasos con los dos botones.
///
/// EL ORDEN DE LOS DOS PASOS IMPORTA: primero quitar, después agregar. Al revés, el horario nuevo choca con el
/// viejo —que todavía está cargado— y el dominio lo rechaza por superposición.
/// </remarks>
internal partial class GestionHorariosViewModel : ObservableValidator
{
	/// <summary>Título de todos los avisos de error de la pantalla.</summary>
	private const string TituloDeLaPantalla = "Horarios";

	#region Services
	private readonly IServicioCatedra _servicioCatedra;
	private readonly IDialogService _dialogService;
	private readonly INavigationService _gestionCatedrasNavigationService;
	#endregion

	#region Store
	private readonly MateriaStore _materiaStore;
	private readonly CatedraStore _catedraStore;
	#endregion

	[ObservableProperty]
	private MateriaSeleccionada? _materia;

	[ObservableProperty]
	private ObservableCollection<HorarioViewModel> _horarios = new();

	/// <summary>Fila elegida en la grilla. Es lo que se quita y lo que se copia al formulario para corregir.</summary>
	[NotifyCanExecuteChangedFor(nameof(QuitarHorarioCommand))]
	[ObservableProperty]
	private HorarioViewModel? _horario;

	#region Formulario
	[NotifyCanExecuteChangedFor(nameof(AgregarHorarioCommand))]
	[ObservableProperty]
	private string _turno = string.Empty;

	[NotifyCanExecuteChangedFor(nameof(AgregarHorarioCommand))]
	[ObservableProperty]
	private string _dia = string.Empty;

	/// <summary>
	/// Hora de inicio del horario nuevo, escrita como <c>HH:mm</c>. La validación de acá es DE FORMA y no
	/// reemplaza a la del dominio: que la hora caiga dentro de los límites del turno elegido, que no se
	/// superponga con otro horario de la misma división y que el docente no quede en dos aulas a la vez son
	/// reglas que dependen del estado de la base y las decide <c>AgregarHorarioAsync</c>. Lo único que esto
	/// evita es mandar a <c>Core</c> un texto que ni siquiera es una hora.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(AgregarHorarioCommand))]
	[NotifyDataErrorInfo]
	[Required(ErrorMessage = "Indique la hora de inicio.")]
	[RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "La hora se escribe como HH:mm.")]
	[ObservableProperty]
	private string _horaInicio = string.Empty;

	/// <summary>
	/// Duración de la hora cátedra. El dominio exige múltiplo de cinco y no menos de treinta minutos, así que
	/// el valor que viene propuesto cumple las dos cosas; el resto lo rechaza <c>AgregarHorarioAsync</c>.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(AgregarHorarioCommand))]
	[ObservableProperty]
	private int _duracionMinutos = 40;

	/// <summary>Opciones del ComboBox de turno: los tres del dominio.</summary>
	[ObservableProperty]
	private ObservableCollection<string> _turnos = new();

	/// <summary>
	/// Opciones del ComboBox de día: LUNES A VIERNES solamente. El dominio rechaza sábado y domingo, así que
	/// no se ofrecen. Esto es presentación, no validación: la regla sigue viviendo en el dominio.
	/// </summary>
	[ObservableProperty]
	private ObservableCollection<string> _dias = new();
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCommand { get; }
	public IAsyncRelayCommand AgregarHorarioCommand { get; }
	public IAsyncRelayCommand QuitarHorarioCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion


	public GestionHorariosViewModel(INavigationService gestionCatedrasNavigationService,
									IServicioCatedra servicioCatedra,
									MateriaStore materiaStore,
									CatedraStore catedraStore,
									IDialogService dialogService)
	{
		_gestionCatedrasNavigationService = gestionCatedrasNavigationService;
		_servicioCatedra = servicioCatedra;

		_materiaStore = materiaStore;
		_catedraStore = catedraStore;
		_dialogService = dialogService;

		Materia = _materiaStore.Materia;

		foreach (var turno in Enum.GetNames<TurnoDelDominio>())
		{
			Turnos.Add(turno);
		}

		foreach (var dia in new[] { DiaDelDominio.Lunes, DiaDelDominio.Martes, DiaDelDominio.Miercoles, DiaDelDominio.Jueves, DiaDelDominio.Viernes })
		{
			Dias.Add(dia.ToString());
		}

		CargarCommand = new AsyncRelayCommand(ExecuteCargarCommand, CanExecuteCargarCommand);
		AgregarHorarioCommand = new AsyncRelayCommand(ExecuteAgregarHorarioCommand, CanExecuteAgregarHorarioCommand);
		QuitarHorarioCommand = new AsyncRelayCommand(ExecuteQuitarHorarioCommand, CanExecuteQuitarHorarioCommand);
		VolverCommand = new RelayCommand(ExecuteVolverCommand, CanExecuteVolverCommand);
	}

	#region CargarCommand
	private bool CanExecuteCargarCommand()
	{
		return _catedraStore.Catedra != Guid.Empty;
	}

	private async Task ExecuteCargarCommand()
	{
		try
		{
			await CargarHorariosAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TituloDeLaPantalla);
		}
	}

	/// <summary>
	/// Relectura de la grilla. La comparten los tres comandos: cargar la pantalla y los dos de escritura
	/// cuando terminan.
	/// </summary>
	private async Task CargarHorariosAsync()
	{
		var horarios = await _servicioCatedra.ListarHorariosSegunCatedraAsync(new ListarHorariosRequest(CatedraID: _catedraStore.Catedra));

		Horarios.Clear();
		foreach (var horario in horarios)
		{
			Horarios.Add(new HorarioViewModel(horario));
		}

		Horario = null;
	}
	#endregion

	#region AgregarHorarioCommand
	private bool CanExecuteAgregarHorarioCommand()
	{
		return _catedraStore.Catedra != Guid.Empty
			   && !string.IsNullOrWhiteSpace(Turno)
			   && !string.IsNullOrWhiteSpace(Dia)
			   && !string.IsNullOrWhiteSpace(HoraInicio)
			   && !HasErrors
			   && DuracionMinutos > 0;
	}

	private async Task ExecuteAgregarHorarioCommand()
	{
		try
		{
			// Si el texto no es una hora no hay nada que mandarle a Core: la validación de forma ya se lo dijo
			// al usuario y el request pide un TimeOnly, no un string.
			if (!TimeOnly.TryParseExact(HoraInicio, "HH:mm", out var hora))
			{
				return;
			}

			await _servicioCatedra.AgregarHorarioAsync(new AgregarHorarioRequest(CatedraID: _catedraStore.Catedra,
																				 Turno: Turno,
																				 DiaSemana: Dia,
																				 HoraInicio: hora,
																				 DuracionHoraCatedra: DuracionMinutos));

			_dialogService.MostrarInformacion($"Se agregó el horario del día { Dia } a las { HoraInicio } de { DuracionMinutos } minutos.",
											  "Operación exitosa");

			await CargarHorariosAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TituloDeLaPantalla);
		}
	}
	#endregion

	#region QuitarHorarioCommand
	private bool CanExecuteQuitarHorarioCommand()
	{
		return Horario is not null;
	}

	private async Task ExecuteQuitarHorarioCommand()
	{
		try
		{
			if (Horario is null)
			{
				return;
			}

			string messageBoxText = $"¿Está seguro que desea quitar el horario del día { Horario.Dia } a las { Horario.HoraInicio } " +
									$"de { Horario.DuracionMinutos } minutos?\n" +
									$"El horario deja de estar cargado en la cátedra.";

			if (!_dialogService.Confirmar(messageBoxText, "Quitar horario"))
			{
				return;
			}

			// HoraInicioValor y DuracionMinutos, no las propiedades string de la grilla: el dominio reconstruye
			// el horario con el TimeOnly de inicio y la duración en minutos.
			await _servicioCatedra.QuitarHorarioAsync(new QuitarHorarioRequest(CatedraID: _catedraStore.Catedra,
																			   Turno: Horario.Turno,
																			   DiaSemana: Horario.Dia,
																			   HoraInicio: Horario.HoraInicioValor,
																			   DuracionHoraCatedra: Horario.DuracionMinutos));

			_dialogService.MostrarInformacion("Se quitó correctamente el horario de la cátedra.", "Operación exitosa");

			await CargarHorariosAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TituloDeLaPantalla);
		}
	}
	#endregion

	#region VolverCommand
	private bool CanExecuteVolverCommand()
	{
		return true;
	}

	private void ExecuteVolverCommand()
	{
		_gestionCatedrasNavigationService.Navigate();
	}
	#endregion

	#region Modificar = quitar + agregar
	/// <summary>
	/// Copia la fila elegida al formulario. Es todo el recorrido de "modificar": el usuario elige el horario,
	/// lo corrige acá y confirma los dos pasos —primero quitar, después agregar—.
	/// </summary>
	/// <remarks>
	/// Con <c>value</c> nulo no limpia nada: <see cref="CargarHorariosAsync"/> deja la selección en nulo
	/// después de recargar, y vaciar el formulario ahí borraría lo que el usuario acaba de escribir.
	/// </remarks>
	partial void OnHorarioChanged(HorarioViewModel? value)
	{
		if (value is null)
		{
			return;
		}

		Turno = value.Turno;
		Dia = value.Dia;
		HoraInicio = value.HoraInicioValor.ToString("HH:mm");
		DuracionMinutos = value.DuracionMinutos;
	}
	#endregion
}
