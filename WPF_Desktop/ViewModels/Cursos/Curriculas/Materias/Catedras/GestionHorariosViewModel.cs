using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
internal partial class GestionHorariosViewModel : ObservableValidator, IUbicacion
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

	#region Presupuesto de la cátedra
	/// <summary>Horas cátedra semanales que la materia declara para esta cátedra.</summary>
	/// <remarks>
	/// EL TOPE SE DESCUBRÍA FALLANDO. <c>Catedra.AgregarHorario</c> rechaza con
	/// <c>HorasCatedraCompletasException</c> cuando <c>HorasAsignadas >= CargaHoraria</c>, y esta
	/// pantalla no mostraba ninguno de los dos números: el usuario cargaba horarios hasta que uno
	/// reventaba. Es la misma regla que docs/ui-ux.md ya aplicó en Diseño curricular —"9 de 14
	/// espacios · 27 de 40 horas"—: el presupuesto se lee, no se descubre chocándose.
	/// </remarks>
	[ObservableProperty]
	private int _cargaHoraria;

	/// <summary>Horas que ya tienen lugar en la semana. Es <c>Catedra.HorasAsignadas</c>, o sea la cantidad de horarios cargados.</summary>
	[ObservableProperty]
	private int _horasConHorario;

	/// <summary>Horas que todavía no tienen lugar en la semana.</summary>
	[ObservableProperty]
	private int _horasSinAsignar;

	/// <summary>
	/// La semana de la cátedra está completa: el dominio no acepta un horario más.
	/// </summary>
	/// <remarks>
	/// Refleja <c>Catedra.TieneHorasCompletas()</c>, pero exige además conocer la carga
	/// (<c>CargaHoraria > 0</c>). <c>CatedraResponse.CargaHoraria</c> es opcional, así que si la
	/// lectura no la trae la pantalla FALLA ABIERTA —deja intentar y que conteste el dominio— en
	/// lugar de apagar el alta sobre un dato que no tiene.
	/// </remarks>
	[NotifyCanExecuteChangedFor(nameof(AgregarHorarioCommand))]
	[ObservableProperty]
	private bool _horasCompletas;
	#endregion

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

		Ruta = new[]
		{
			Migaja.Contexto("CURSOS"),
			Migaja.Contexto("DISEÑO CURRICULAR"),
			Migaja.Hacia($"{ Materia?.Descripcion?.ToUpper() } · CÁTEDRAS", VolverCommand),
			Migaja.Actual("HORARIOS")
		};
	}

	/// <summary>
	/// Ruta hasta los horarios de la cátedra: cuatro niveles, que es exactamente la profundidad
	/// del drill-down que no tenía ninguna indicación de ubicación.
	/// </summary>
	/// <remarks>
	/// "CURSOS" y "DISEÑO CURRICULAR" van como contexto y no como enlace: esta pantalla sólo
	/// sabe volver a las cátedras de su materia, que es el único servicio de navegación que
	/// recibe. Prometer un salto de dos niveles exigiría inyectarle navegación que no usa.
	/// </remarks>
	public IReadOnlyList<Migaja> Ruta { get; }

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

		await CargarPresupuestoAsync();
	}

	/// <summary>
	/// Trae la carga horaria de la cátedra y arma con ella los tres números de la tira.
	/// </summary>
	/// <remarks>
	/// EN LA MISMA CADENA ESPERADA que los horarios y nunca en paralelo: el <c>DbContext</c> no es
	/// reentrante y dos consultas en vuelo lo rompen.
	///
	/// Se pide por materia y se filtra por la cátedra del store porque <c>IServicioCatedra</c> no
	/// expone la lectura de UNA cátedra: las tres consultas de cátedras son por materia, por
	/// división y por docente. Es una consulta de más por carga de pantalla; la alternativa era
	/// agregarle a Core un caso de uso nuevo para un número que ya viaja en el <c>Response</c>.
	/// </remarks>
	private async Task CargarPresupuestoAsync()
	{
		HorasConHorario = Horarios.Count;

		if (Materia is null)
		{
			return;
		}

		var catedras = await _servicioCatedra.ListarCatedrasSegunMateriaAsync(
			new ListarCatedrasSegunMateriaRequest(MateriaID: Materia.MateriaID));

		var catedra = catedras.FirstOrDefault(x => x.CatedraID == _catedraStore.Catedra);

		CargaHoraria = catedra?.CargaHoraria ?? 0;
		HorasSinAsignar = Math.Max(0, CargaHoraria - HorasConHorario);
		HorasCompletas = CargaHoraria > 0 && HorasConHorario >= CargaHoraria;
	}
	#endregion

	#region AgregarHorarioCommand
	private bool CanExecuteAgregarHorarioCommand()
	{
		// !HorasCompletas es el gate del dominio adelantado: con la semana llena AgregarHorario
		// tira HorasCatedraCompletasException, así que la acción se apaga en lugar de fallar.
		return _catedraStore.Catedra != Guid.Empty
			   && !HorasCompletas
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

			// El título lleva la operación y el cuerpo el detalle, nunca "Operación exitosa": el
			// cartel tiene que decir QUÉ pasó sin que haya que leerlo entero.
			_dialogService.MostrarInformacion($"{ Dia } a las { HoraInicio }, { DuracionMinutos } minutos en el turno { Turno.ToLower() }.",
											  "Horario agregado");

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

			if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
					titulo: $"Quitar el horario del { Horario.Dia } a las { Horario.HoraInicio }",
					consecuencia: $"La cátedra deja de tener esa hora de { Horario.DuracionMinutos } minutos en el turno { Horario.Turno }, "
								+ "y las horas que queden sin asignar vuelven a estar disponibles para repartir entre los docentes.",
					verbo: "Quitar horario")))
			{
				return;
			}

			// Se leen ANTES de quitar y recargar: CargarHorariosAsync deja Horario en nulo, así que
			// un cartel escrito con la fila después de la recarga se queda sin datos.
			var diaQuitado = Horario.Dia;
			var horaQuitada = Horario.HoraInicio;

			// HoraInicioValor y DuracionMinutos, no las propiedades string de la grilla: el dominio reconstruye
			// el horario con el TimeOnly de inicio y la duración en minutos.
			await _servicioCatedra.QuitarHorarioAsync(new QuitarHorarioRequest(CatedraID: _catedraStore.Catedra,
																			   Turno: Horario.Turno,
																			   DiaSemana: Horario.Dia,
																			   HoraInicio: Horario.HoraInicioValor,
																			   DuracionHoraCatedra: Horario.DuracionMinutos));

			_dialogService.MostrarInformacion($"La cátedra ya no tiene el { diaQuitado } a las { horaQuitada }.", "Horario quitado");

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
