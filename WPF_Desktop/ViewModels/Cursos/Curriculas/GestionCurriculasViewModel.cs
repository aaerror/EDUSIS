using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioMaterias;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas;

internal partial class GestionCurriculasViewModel : ObservableValidator, IUbicacion
{
	#region Services
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionCatedrasNavigationService;
	private readonly IServicioCurricula _servicioCurricula;
	private readonly IServicioMateria _servicioMateria;
	private readonly IDialogService _dialogService;
	#endregion

	#region Store
	private readonly CursoStore _cursoStore;
	private readonly MateriaStore _materiaStore;
	#endregion

	/// <summary>
	/// Tope de horas cátedra que esta pantalla ofrece para UNA materia.
	/// </summary>
	/// <remarks>
	/// Es un límite de la pantalla y no del dominio —<c>Materia</c> acepta cualquier carga positiva—,
	/// y estaba escrito dos veces como una lista fija de 1 a 5 en el XAML. Vive acá para que el combo
	/// lo combine con las horas que la currícula todavía tiene libres.
	/// </remarks>
	private const int MAXIMO_HORAS_POR_MATERIA = 5;

	public string Curso => $"{ _cursoStore.Curso.Grado } año de { _cursoStore.Curso.NivelEducativo }".ToUpper();

	#region Ocupacion de la curricula
	/// <remarks>
	/// EL TOPE DEJA DE SER UNA SORPRESA. <c>TopesCurriculares</c> limita la currícula a 14 espacios y
	/// 40 horas cátedra semanales, y hasta acá la pantalla no mostraba ninguno de los dos números: el
	/// usuario cargaba una materia, el alta fallaba y recién entonces se enteraba de que había un
	/// límite. Peor todavía, el combo de horas ofrecía siempre 1 a 5, así que con 38 horas consumidas
	/// dejaba elegir 5 y rechazaba la operación.
	///
	/// Los cuatro números salen de <c>ConsultarOcupacionAsync</c>, que lee los MISMOS contadores que
	/// <c>ServicioMateria</c> le pasa a <c>TopesCurriculares</c> antes de un alta. Que los dos caminos
	/// lean lo mismo es lo que evita que la pantalla prometa un cupo que el alta después niega.
	/// </remarks>
	[ObservableProperty]
	private int _espaciosOcupados;

	[ObservableProperty]
	private int _espaciosMaximos;

	[ObservableProperty]
	private int _horasOcupadas;

	[ObservableProperty]
	private int _horasMaximas;

	[ObservableProperty]
	private int _horasDisponibles;

	/// <summary>La currícula admite al menos una materia más. Es la precondición del alta.</summary>
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private bool _admiteOtraMateria;

	/// <summary>
	/// Las cargas horarias que la currícula todavía admite. El combo se arma con esto en vez de con
	/// una lista fija de 1 a 5: ofrecer una carga que no entra es ofrecer un error.
	/// </summary>
	[ObservableProperty]
	private ObservableCollection<int> _opcionesCargaHoraria = new();

	/// <summary>La currícula elegida está vigente y por lo tanto acepta cambios en sus materias.</summary>
	/// <remarks>
	/// <c>ServicioMateria</c> exige currícula vigente para alta, modificación y baja. La grilla lista
	/// también las cerradas —son el registro de los años anteriores— y sobre ellas los cinco botones
	/// quedaban encendidos hasta que Core contestaba <c>CurriculaNoVigenteException</c>.
	/// </remarks>
	public bool CurriculaVigente => Curricula is not null && Curricula.Vigente;

	/// <summary>
	/// La currícula está vigente pero ya no entra nada más. Es el aviso del tope.
	/// </summary>
	/// <remarks>
	/// Es estado derivado y vive acá, no en la vista, porque los dos avisos se excluyen: una currícula
	/// cerrada no se mide por cupo, y mostrarle el tope sería contestarle una pregunta que no hizo.
	/// Expresarlo en XAML pediría un <c>MultiBinding</c> con un converter multivalor que este tema no
	/// tiene, y además cuál de los dos avisos corresponde es lógica de presentación, no de dibujo.
	/// </remarks>
	public bool MostrarAvisoSinCupo => CurriculaVigente && !AdmiteOtraMateria;

	partial void OnAdmiteOtraMateriaChanged(bool value)
	{
		OnPropertyChanged(nameof(MostrarAvisoSinCupo));
	}
	#endregion

	#region Registrar Materia
	[Required(AllowEmptyStrings=true, ErrorMessage="Se debe especificar el nombre de la materia.")]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private string _descripcion = string.Empty;

	[Required]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private int _cargaHoraria = 1;
	#endregion

	#region Notifications
	[ObservableProperty]
	private string _mensaje = string.Empty;

	[ObservableProperty]
	private bool _habilitarNotificacion;
	#endregion

	/// <remarks>
	/// Se fue <c>HabilitarMaterias</c>: se escribía en tres lugares y no estaba enlazada desde ningún
	/// XAML ni leída por nadie. La visibilidad de la lista de materias la decide la tarjeta que la
	/// contiene, que cuelga de <see cref="HabilitarCurriculas"/>.
	/// </remarks>
	[ObservableProperty]
	private bool _habilitarCurriculas = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarRegistrarMateria = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarEditarMateria = false;

	#region Curriculas
	[ObservableProperty]
	public ObservableCollection<CurriculaViewModel> _curriculas = new();

	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private CurriculaViewModel _curricula;

	/// <summary>
	/// Elegir una currícula cambia qué se puede hacer con ella; traer sus datos es otra cosa.
	/// </summary>
	/// <remarks>
	/// ACÁ NO SE DISPARA NINGUNA CONSULTA, y es a propósito. Este setter corre desde el
	/// <c>SelectionChanged</c> de la grilla, que YA está lanzando <c>CargarMateriasCommandAsync</c>
	/// por su propio trigger. Lanzar además la ocupación desde acá —sin poder esperarla, porque un
	/// <c>partial void</c> no es asíncrono— ponía dos consultas en vuelo contra el mismo
	/// <c>DbContext</c>, que no es reentrante: la segunda moría con "A second operation was started
	/// on this context instance before a previous operation completed".
	///
	/// La ocupación se pide al final de <see cref="CargarMateriasAsync"/>, en la misma cadena
	/// esperada. Las dos cosas describen a la misma currícula y se leen juntas.
	/// </remarks>
	partial void OnCurriculaChanged(CurriculaViewModel value)
	{
		OnPropertyChanged(nameof(CurriculaVigente));
		OnPropertyChanged(nameof(MostrarAvisoSinCupo));
	}
	#endregion

	#region Materias
	[ObservableProperty]
	public ObservableCollection<MateriaViewModel> _materias = new();

	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private MateriaViewModel _materia;

	/// <summary>
	/// Sigue los errores de validación de la materia elegida.
	/// </summary>
	/// <remarks>
	/// <c>CanExecuteGuardarCommand("Update")</c> consulta <c>Materia.HasErrors</c>, que vive en OTRO
	/// ViewModel: sin esta suscripción el comando no se reevaluaba cuando esos errores aparecían o se
	/// corregían, así que el botón de guardar quedaba con el estado que tenía al abrir el editor
	/// —habilitado sobre una materia que ya no era válida, o escondido después de corregirla—.
	/// </remarks>
	partial void OnMateriaChanged(MateriaViewModel oldValue, MateriaViewModel newValue)
	{
		if (oldValue is not null)
		{
			oldValue.ErrorsChanged -= OnErroresDeLaMateriaChanged;
		}

		if (newValue is not null)
		{
			newValue.ErrorsChanged += OnErroresDeLaMateriaChanged;
		}
	}

	private void OnErroresDeLaMateriaChanged(object sender, DataErrorsChangedEventArgs e)
	{
		GuardarCommandAsync.NotifyCanExecuteChanged();
	}
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCurriculasCommandAsync { get; }
	public IAsyncRelayCommand CargarMateriasCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommand { get; }
	public IAsyncRelayCommand RegistrarCommand { get; }

	public IRelayCommand EditarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionCurriculasViewModel(INavigationService gestionCursosNavigationService,
									  INavigationService gestionCatedrasNavigationService,
									  IServicioCurricula servicioCurricula,
									  IServicioMateria servicioMateria,
									  IDialogService dialogService,
									  CursoStore cursoStore,
									  MateriaStore materiaStore)
	{
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionCatedrasNavigationService = gestionCatedrasNavigationService;
		_servicioCurricula = servicioCurricula;
		_servicioMateria = servicioMateria;
		_dialogService = dialogService;
		_cursoStore = cursoStore;
		_materiaStore = materiaStore;

		CargarCurriculasCommandAsync = new AsyncRelayCommand(CargarCurriculasAsync);
		CargarMateriasCommandAsync = new AsyncRelayCommand(CargarMateriasAsync);

		CancelarCommand = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, CanExecuteEditarCommand);
		RegistrarCommand = new AsyncRelayCommand<string>(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommand);

		HabilitarNotificacion = false;
		HabilitarEditarMateria = false;

		VolverCommand = new RelayCommand(gestionCursosNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("CURSOS", VolverCommand),
			Migaja.Actual($"{ Curso } · DISEÑO CURRICULAR")
		};
	}

	/// <summary>
	/// Ruta hasta el diseño curricular. El curso vive acá y no en el título: el título dice qué
	/// es la pantalla y antes decía el grado, con lo cual la palabra "diseño curricular" no
	/// aparecía en ninguna parte.
	/// </summary>
	public IReadOnlyList<Migaja> Ruta { get; }

	public IRelayCommand VolverCommand { get; }

	public async Task CargarCurriculasAsync()
	{
		Materias.Clear();
		Curriculas.Clear();
		try
		{
			var request = new CursoRequest(_cursoStore.Curso.CursoID);
			var response = await _servicioCurricula.ListarCurriculasSegunCursoAsync(request);

			Curriculas = new ObservableCollection<CurriculaViewModel>(response.Select(x => new CurriculaViewModel(x)));

			// Un curso sin currículas deja la tarjeta A LA VISTA con la grilla vacía, que ya trae su
			// frase y dice qué hacer. Antes se colapsaba la tarjeta entera y se la reemplazaba por un
			// aviso, de modo que el estado vacío de la grilla era código inalcanzable.
			HabilitarCurriculas = true;
			HabilitarNotificacion = false;
		}
		catch (Exception ex)
		{
			// Un fallo de carga es un error, no una advertencia: es la única pista de por qué la
			// pantalla está vacía, y un aviso al pie de 8 segundos se pierde de vista.
			_dialogService.MostrarError($"No se pudieron traer los diseños curriculares del curso. { ex.Message }", "Diseño curricular");

			HabilitarCurriculas = true;
			HabilitarNotificacion = false;
		}
	}

	/// <summary>
	/// Trae cuánto de la currícula elegida está ocupado, y arma con eso las cargas horarias que el
	/// combo del alta puede ofrecer.
	/// </summary>
	/// <remarks>
	/// Sin currícula elegida deja todo en cero y el alta apagada, que es lo correcto: no hay currícula
	/// de la cual informar un cupo.
	/// </remarks>
	public async Task CargarOcupacionAsync()
	{
		if (Curricula is null)
		{
			EspaciosOcupados = 0;
			EspaciosMaximos = 0;
			HorasOcupadas = 0;
			HorasMaximas = 0;
			HorasDisponibles = 0;
			AdmiteOtraMateria = false;
			OpcionesCargaHoraria = new ObservableCollection<int>();

			return;
		}

		try
		{
			var ocupacion = await _servicioCurricula.ConsultarOcupacionAsync(
				new CurriculaRequest(CursoID: Curricula.CursoID, CurriculaID: Curricula.CurriculaID));

			EspaciosOcupados = ocupacion.EspaciosOcupados;
			EspaciosMaximos = ocupacion.EspaciosMaximos;
			HorasOcupadas = ocupacion.HorasOcupadas;
			HorasMaximas = ocupacion.HorasMaximas;
			HorasDisponibles = ocupacion.HorasDisponibles;
			AdmiteOtraMateria = ocupacion.AdmiteOtraMateria;

			// El tope de 5 horas por materia es de la pantalla; el de horas libres, de la currícula.
			// Manda el más chico de los dos.
			var maximo = Math.Min(MAXIMO_HORAS_POR_MATERIA, ocupacion.HorasDisponibles);
			OpcionesCargaHoraria = new ObservableCollection<int>(Enumerable.Range(1, Math.Max(0, maximo)));

			if (!OpcionesCargaHoraria.Contains(CargaHoraria))
			{
				CargaHoraria = OpcionesCargaHoraria.FirstOrDefault();
			}

			// Editar es otra cuenta que dar de alta: cambiar una materia de 3 a 4 horas usa UNA hora
			// más, no cuatro, así que su tope es lo libre MÁS lo que ella ya ocupa. Cada materia
			// recibe el suyo, y el método conserva siempre su valor actual.
			foreach (var materia in Materias)
			{
				var propias = materia.HorasCatedra ?? 0;

				materia.OfrecerCargasHorariaHasta(Math.Min(MAXIMO_HORAS_POR_MATERIA, ocupacion.HorasDisponibles + propias));
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError($"No se pudo calcular el cupo de la currícula. { ex.Message }", "Diseño curricular");
		}
	}

	/// <remarks>
	/// Termina pidiendo la ocupación de la currícula, EN LA MISMA CADENA ESPERADA y no en paralelo:
	/// las materias y el cupo describen a la misma currícula, y el <c>DbContext</c> no admite dos
	/// consultas simultáneas. Ver el <c>remarks</c> de <c>OnCurriculaChanged</c>.
	/// </remarks>
	public async Task CargarMateriasAsync()
	{
		Materias.Clear();
		if (Curricula is not null)
		{
			try
			{
				var request = new ListarMateriasSegunCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	  CurriculaID: Curricula.CurriculaID);
				var response = await _servicioMateria.ListarMateriasSegunCurriculaAsync(request);

				// Sin el "if (Count is not 0)": una currícula recién creada tiene cero materias y ese
				// camino no hacía nada, así que la lista quedaba mostrando las de la currícula anterior
				// hasta que alguien cargara una. La lista vacía la explica el estado vacío de la lista.
				Materias = new ObservableCollection<MateriaViewModel>(response.Select(x => new MateriaViewModel(x)));

				HabilitarNotificacion = false;
			}
			catch (Exception ex)
			{
				_dialogService.MostrarError($"No se pudieron traer las materias de la currícula. { ex.Message }", "Diseño curricular");

				HabilitarNotificacion = false;
			}
		}

		// Siempre, y al final: sin currícula elegida la consulta deja el cupo en cero y apaga el
		// alta, que es exactamente lo que corresponde mostrar.
		await CargarOcupacionAsync();
	}

	#region Commands
	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Cursos" => !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		"Catedras" => Materia is not null && !HabilitarEditarMateria,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Cursos":
				_gestionCursosNavigationService.Navigate();
				break;

			case "Catedras":
				_materiaStore.Materia = new MateriaSeleccionada(Materia.CursoID, Materia.MateriaID, Materia.Descripcion);
				_gestionCatedrasNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region GuardarCommandAsync
	/// <remarks>
	/// "Update" mira el <c>HasErrors</c> DE LA MATERIA, no el de esta pantalla: el formulario de
	/// edición trabaja sobre el <c>MateriaViewModel</c> de la fila, así que las reglas que se violan
	/// son las suyas. Antes sólo exigía que el editor estuviera abierto, y con la descripción vacía el
	/// botón quedaba vivo hasta que <c>Materia.EditarDescripcion</c> tiraba <c>ArgumentNullException</c>.
	/// </remarks>
	private bool CanExecuteGuardarCommand(object obj) => obj switch
	{
		"Materia" => !HasErrors && CurriculaVigente && AdmiteOtraMateria,
		"Update" => HabilitarEditarMateria && CurriculaVigente && Materia is not null && !Materia.HasErrors,
		_ => false
	};

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Materia":
				if (string.IsNullOrWhiteSpace(Descripcion))
				{
					_dialogService.MostrarAdvertencia("Escribí el nombre de la materia antes de agregarla.", "Falta el nombre de la materia");

					break;
				}

				// La carga horaria viaja en el texto y la guarda estática W5 lo comprueba: el
				// número que el usuario lee en el diálogo tiene que ser el que sale en el request.
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Agregar { Descripcion } al diseño curricular",
						mensaje: $"Queda en { Grados.Decir(_cursoStore.Curso.Grado) } de { _cursoStore.Curso.NivelEducativo }. "
							   + $"Carga horaria: { CargaHoraria } horas cátedra.",
						verbo: "Agregar materia")))
				{
					try
					{
						var request = new RegistrarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  Descripcion: Descripcion,
																  HorasCatedra: CargaHoraria);
						await _servicioMateria.RegistrarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Descripcion }, { CargaHoraria } horas cátedra.", "Materia agregada");

						// El formulario se vacía y se cierra SÓLO si el alta salió bien.
						Descripcion = string.Empty;
						CargaHoraria = 1;
						CerrarAltaDeMateria();

						// CargarMateriasAsync termina pidiendo la ocupación: una sola cadena.
						await CargarMateriasAsync();
					}
					catch (Exception ex)
					{
						// NO se cierra el formulario ni se borra lo escrito. El alta puede fallar por
						// el tope de horas o por nombre repetido, y las dos veces lo que corresponde
						// es corregir el dato que está a la vista: cerrar el formulario y limpiarlo
						// obligaba a escribir todo de nuevo para cambiar un número.
						_dialogService.MostrarError(ex.Message, "No se pudo agregar la materia");

						// El rechazo puede venir justamente de que el cupo cambió: se lo vuelve a leer.
						await CargarOcupacionAsync();
					}
				}

				break;
			case "Update":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Guardar los cambios de { Materia.Descripcion }",
						mensaje: $"Carga horaria: { Materia.HorasCatedra?.ToString() ?? "sin definir" } horas cátedra. "
							   + "La carga horaria es la que reparten las cátedras de la materia entre sus docentes.",
						verbo: "Guardar materia")))
				{
					try
					{
						var request = new ModificarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  MateriaID: Materia.MateriaID,
																  Descripcion: Materia.Descripcion,
																  HorasCatedra: Materia.HorasCatedra.HasValue ? Materia.HorasCatedra.Value : 0);
						await _servicioMateria.ModificarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Materia.Descripcion }.", "Materia actualizada");

						HabilitarEditarMateria = false;
						await CargarMateriasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo guardar la materia");

						// RECARGAR ES OBLIGATORIO ACÁ. El formulario de edición trabaja sobre el
						// MateriaViewModel de la fila, así que el valor rechazado ya está escrito en
						// la lista: sin esta recarga la grilla sigue mostrando como guardado algo que
						// Core no aceptó.
						HabilitarEditarMateria = false;
						await CargarMateriasAsync();
					}
				}
				break;
		}
	}
	#endregion

	#region EditarCommand
	private bool CanExecuteEditarCommand(object obj) => obj switch
	{
		"Materia" => Materia is not null && CurriculaVigente && !HabilitarEditarMateria,
		_ => false
	};

	private void ExecuteEditarCommand(object obj)
	{
		switch (obj)
		{
			case "Materia":
				HabilitarRegistrarMateria = false;
				HabilitarEditarMateria = true;

				break;
		}
	}
	#endregion

	#region RegistrarCommand
	/// <remarks>
	/// "Materia" exige ahora currícula VIGENTE y cupo disponible, que son las dos reglas con las que
	/// Core rechaza el alta —<c>CurriculaNoVigenteException</c> y los dos topes de
	/// <c>TopesCurriculares</c>—. Antes el botón sólo miraba que hubiera una currícula elegida, así
	/// que sobre una currícula cerrada o llena abría un formulario que no podía terminar.
	/// </remarks>
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"Curricula" => !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		"Materia" => CurriculaVigente && AdmiteOtraMateria && !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteRegistrarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Curricula":
				// Destructiva y no Revisar: abrir una currícula CIERRA la vigente, y cerrar un período
				// es uno de los casos que el contrato manda preguntar en serio. Lo que se pierde no
				// son las materias —quedan como registro— sino la posibilidad de seguir editándolas.
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: "Abrir un diseño curricular nuevo",
						consecuencia: $"El diseño curricular vigente se cierra hoy, { DateTime.Today.ToString("D") }, y deja de aceptar cambios: "
									+ "sus materias quedan como registro de los años anteriores y ya no se podrán agregar, editar ni quitar. "
									+ "El diseño nuevo empieza vacío.",
						verbo: "Crear currícula")))
				{
					try
					{
						var request = new RegistrarCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	FechaInicio: DateTime.Today,
																	FechaFin: null);
						await _servicioCurricula.RegistrarCurriculaAsync(request);

						_dialogService.MostrarInformacion($"Vigente desde hoy, { DateTime.Today.ToString("d") }. Ya podés agregarle materias.", "Currícula creada");

						HabilitarEditarMateria = false;
						await CargarCurriculasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				break;

			case "Update":
				HabilitarRegistrarMateria = false;
				HabilitarEditarMateria = true;

				break;

			case "Materia":
				HabilitarNotificacion = false;

				// La tarjeta de currículas y materias se oculta mientras dura el alta, para que el
				// formulario quede solo en pantalla. CerrarAltaDeMateria es el único camino de vuelta
				// y por eso TODAS las salidas del alta —éxito, cancelación y error— pasan por él.
				HabilitarCurriculas = false;
				HabilitarRegistrarMateria = true;
				break;
		}
	}

	/// <summary>
	/// Cierra el formulario de alta y devuelve a la vista la tarjeta de currículas y materias.
	/// </summary>
	/// <remarks>
	/// ES EL ARREGLO DE UNA PANTALLA QUE SE QUEDABA EN BLANCO. Abrir el alta apagaba
	/// <c>HabilitarCurriculas</c>, que no sólo deshabilita la tarjeta sino que la COLAPSA —su
	/// <c>Visibility</c> cuelga de su propio <c>IsEnabled</c>—, y nadie la volvía a encender: ni el
	/// alta exitosa ni la cancelación, que llamaban a <c>CargarMateriasAsync</c> cuando el único lugar
	/// que la restauraba era <c>CargarCurriculasAsync</c>. Después de agregar una materia quedaba el
	/// encabezado y nada debajo hasta volver a navegar a la pantalla.
	/// </remarks>
	private void CerrarAltaDeMateria()
	{
		HabilitarRegistrarMateria = false;
		HabilitarCurriculas = true;
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Materia" => Materia is not null && CurriculaVigente && !HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Materia":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Quitar { Materia.Descripcion } del diseño curricular",
						consecuencia: "La materia deja de figurar en el diseño curricular del curso y no se le podrán cargar "
									+ "calificaciones. Core rechaza la operación si la materia ya tiene cátedras con docentes designados.",
						verbo: "Quitar materia")))
				{
					try
					{
						var request = new EliminarMateriaRequest(CursoID: Curricula.CursoID,
																 CurriculaID: Curricula.CurriculaID,
																 MateriaID: Materia.MateriaID);
						await _servicioMateria.EliminarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Materia.Descripcion }.", "Materia quitada");

						await CargarMateriasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}
				break;
		}
	}
	#endregion

	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) => obj switch
	{
		"Nueva" => HabilitarRegistrarMateria,
		"Editar" => HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteCancelarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Nueva":
				CerrarAltaDeMateria();

				Descripcion = string.Empty;
				CargaHoraria = OpcionesCargaHoraria.FirstOrDefault();

				await CargarMateriasAsync();
				break;

			case "Editar":
				HabilitarEditarMateria = false;
				await CargarMateriasAsync();
				break;

			default:
				break;
		}
	}
	#endregion
	#endregion
}
