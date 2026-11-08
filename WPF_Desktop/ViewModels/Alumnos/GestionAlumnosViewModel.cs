using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

/// <summary>
/// Índice + ficha de alumnos.
/// </summary>
/// <remarks>
/// <para>
/// ALUMNOS NO TENÍA ÍNDICE, y era una limitación de la capa de aplicación: <c>IServicioAlumno</c>
/// sólo sabía buscar por documento exacto —<c>BuscarPorDNIAsync</c>— y por nombre completo exacto
/// con su coma y sus tildes, y las dos devolvían UNA persona. La pantalla lo decía en un comentario
/// de su propio XAML: "mientras eso sea así, la búsqueda por documento exacto es el único camino".
/// </para>
/// <para>
/// Ahora hay un solo campo que acepta las dos cosas. Cuál de las dos búsquedas se hace lo decide
/// <c>ServicioAlumno.BuscarAlumnosAsync</c> y no este ViewModel: qué significa el dato que alguien
/// tipeó es política de aplicación, y acá no se puede probar sin armar la pantalla con sus seis
/// dependencias.
/// </para>
/// <para>
/// La búsqueda es contra la base y no un filtro sobre una nómina precargada, a diferencia de
/// <c>GestionDocentes</c>: la matrícula de alumnos es un orden de magnitud más grande que el
/// personal docente, y traerla entera en cada entrada a la pantalla para filtrarla en memoria es un
/// costo que crece con cada ciclo lectivo. Por eso hay botón y no filtro en vivo.
/// </para>
/// </remarks>
internal partial class GestionAlumnosViewModel : ObservableValidator
{
	#region Servicio
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly IDialogService _dialogService;
	private readonly INavigationService _registrarAlumnoNavigationService;
	private readonly INavigationService _perfilNavigationService;
	private readonly INavigationService _inscripcionAlumnoNavigationService;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private LegajoStore _perfilStore;

	/// <summary>
	/// El único campo de búsqueda: documento, o parte del nombre o del apellido.
	/// </summary>
	/// <remarks>
	/// La validación que reemplaza era <c>^\d{8}$</c>, es decir: la pantalla rechazaba las letras.
	/// Ahora el mínimo son dos caracteres, no por capricho de forma sino porque con uno solo la
	/// consulta devuelve media institución y la lista deja de servir para elegir.
	/// </remarks>
	[Required(AllowEmptyStrings = true, ErrorMessage = "Escribí un documento, un nombre o un apellido.")]
	[MinLength(2, ErrorMessage = "Escribí al menos dos caracteres.")]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(BuscarCommand))]
	[ObservableProperty]
	private string _termino = string.Empty;

	[ObservableProperty]
	private ObservableCollection<AlumnoEncontradoViewModel> _resultados = new();

	/// <summary>
	/// El alumno elegido en el índice. Escribirlo recarga la ficha y el resumen académico.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private AlumnoEncontradoViewModel _alumnoSeleccionado;

	/// <summary>Qué cursa, su promedio y su asistencia. Nulo mientras no haya alumno elegido.</summary>
	[ObservableProperty]
	private ResumenAcademicoViewModel _resumen;

	/// <summary>Cuántas coincidencias hubo. Lo lee el índice arriba de la lista.</summary>
	[ObservableProperty]
	private string _conteo = string.Empty;

	/// <summary>
	/// Qué hacer o qué pasó. Lo lee el panel derecho mientras no hay alumno elegido.
	/// </summary>
	/// <remarks>
	/// SE FUE <c>ExisteAlumno</c>, el bool que controlaba la visibilidad de la ficha. La vista
	/// ahora deriva el estado de <see cref="AlumnoSeleccionado"/> con el converter
	/// <c>visibleSiHay</c>: un bool paralelo a la selección es un dato que se puede desincronizar
	/// de ella, y el borrado ya lo había desincronizado una vez.
	/// </remarks>
	[ObservableProperty]
	private string _message = string.Empty;

	#region COMMANDs
	public IAsyncRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IAsyncRelayCommand BuscarCommand { get; }
	public IAsyncRelayCommand CargarResumenCommandAsync { get; }
	#endregion


	public GestionAlumnosViewModel(IServicioAlumno servicioAlumnos,
								   IDialogService dialogService,
								   INavigationService registrarAlumnoNavigationService,
								   INavigationService inscripcionAlumnoNavigationService,
								   INavigationService verPerfilNavigationService,
								   LegajoStore perfilBuscadoStore)
	{
		_servicioAlumnos = servicioAlumnos;
		_dialogService = dialogService;
		_registrarAlumnoNavigationService = registrarAlumnoNavigationService;
		_inscripcionAlumnoNavigationService = inscripcionAlumnoNavigationService;
		_perfilNavigationService = verPerfilNavigationService;

		_perfilStore = perfilBuscadoStore;

		BuscarCommand = new AsyncRelayCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
		CargarResumenCommandAsync = new AsyncRelayCommand(CargarResumenAsync);
		EliminarCommand = new AsyncRelayCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		Message = "Buscá un alumno por su documento, su nombre o su apellido para ver su perfil, inscribirlo o quitarlo. Si todavía no está cargado, usá «Registrar alumno».";
	}

	#region BuscarCommand
	private bool CanExecuteBuscarCommand() =>
		!string.IsNullOrWhiteSpace(Termino) && !HasErrors;

	/// <summary>
	/// Trae las coincidencias del término y las deja en el índice.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Elige sola cuando hay UNA coincidencia: buscar un documento exacto y tener que hacerle clic
	/// al único resultado es un paso que no decide nada. Con varias no elige ninguna, porque ahí
	/// sí hay una decisión y es de quien busca.
	/// </para>
	/// <para>
	/// Era <c>async void</c> sobre un <c>RelayCommand</c>: cualquier excepción que no cayera en el
	/// try —por ejemplo al escribir en el store— no la podía atrapar nadie y terminaba en el
	/// manejador global, que apaga la aplicación. Como <c>AsyncRelayCommand</c> además expone
	/// <c>IsRunning</c>, la pantalla puede mostrar que está buscando.
	/// </para>
	/// </remarks>
	private async Task ExecuteBuscarCommand()
	{
		try
		{
			var encontrados = await _servicioAlumnos.BuscarAlumnosAsync(new BuscarAlumnoRequest(Termino));

			AlumnoSeleccionado = null;
			Resultados = new ObservableCollection<AlumnoEncontradoViewModel>(
				encontrados.Select(x => new AlumnoEncontradoViewModel(x)));

			if (Resultados.Count is 0)
			{
				Conteo = string.Empty;
				Message = $"No hay ningún alumno que coincida con «{ Termino }». Revisá el dato o registralo con «Registrar alumno».";

				return;
			}

			Conteo = Contar(Resultados.Count, "coincidencia", "coincidencias");

			if (Resultados.Count is 1)
			{
				AlumnoSeleccionado = Resultados.First();

				return;
			}

			// Con varias coincidencias el panel derecho sigue vacío, así que tiene que decir qué
			// falta. Dejar el mensaje anterior ahí mostraría el texto de la búsqueda pasada.
			Message = $"{ Conteo }. Elegí una para ver su situación académica y su perfil.";
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo buscar el alumno");
		}
	}
	#endregion

	#region Alumno elegido
	/// <summary>
	/// Elegir un alumno escribe el store y recarga su resumen académico.
	/// </summary>
	/// <remarks>
	/// El store se escribe acá y NO al buscar, que es donde se escribía antes: con una lista de
	/// resultados, "el alumno que se buscó" y "el alumno que se eligió" dejaron de ser lo mismo, y
	/// el store es el parámetro de navegación hacia Perfil e Inscripción.
	///
	/// La carga del resumen sale por un comando y no desde acá con <c>async void</c>: es el patrón
	/// de <c>GestionReportesViewModel.OnCursoChanged</c>, y el comando además expone
	/// <c>IsRunning</c> para que la ficha pueda mostrar que está cargando.
	/// </remarks>
	partial void OnAlumnoSeleccionadoChanged(AlumnoEncontradoViewModel value)
	{
		Resumen = null;

		if (value is null)
		{
			_perfilStore.Limpiar();

			return;
		}

		_perfilStore.PersonaID = value.PersonaID;
		_perfilStore.Documento = value.Documento;

		Message = string.Empty;

		CargarResumenCommandAsync.Execute(null);
	}

	/// <summary>
	/// Trae el resumen académico del alumno elegido.
	/// </summary>
	/// <remarks>
	/// Un fallo acá NO borra la ficha ni deselecciona: los datos personales ya están en pantalla y
	/// siguen siendo válidos, así que lo que se pierde es el bloque académico y no la elección. El
	/// aviso va por diálogo porque una ficha con tres rayas y sin explicación no se distingue de un
	/// alumno sin trayectoria.
	/// </remarks>
	private async Task CargarResumenAsync()
	{
		if (AlumnoSeleccionado is null)
		{
			return;
		}

		try
		{
			var resumen = await _servicioAlumnos.ConsultarResumenAcademicoAsync(
				new PersonaRequest(AlumnoSeleccionado.PersonaID));

			Resumen = new ResumenAcademicoViewModel(resumen);
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo cargar la situación académica del alumno");
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Asignar" => AlumnoSeleccionado is not null,
		"Perfil" => AlumnoSeleccionado is not null,
		"Registrar" => true,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Asignar":
				_inscripcionAlumnoNavigationService.Navigate();
				break;

			case "Perfil":
				_perfilNavigationService.Navigate();
				break;

			case "Registrar":
				_registrarAlumnoNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand() =>
		AlumnoSeleccionado is not null;

	/// <summary>
	/// Quita el alumno de la institución, con toda su trayectoria.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Tenía tres problemas. Era <c>async void</c>; la confirmación no decía qué se pierde; y
	/// al terminar NO limpiaba la pantalla: la ficha del alumno recién borrado seguía ahí, con
	/// los botones "Perfil" e "Inscribir" habilitados sobre un registro que ya no existe.
	/// </para>
	/// <para>
	/// El cuarto problema era el peor y no se veía: el texto afirmaba que "las inscripciones y
	/// calificaciones ya registradas quedan en el historial de la división", y eso era
	/// <strong>falso</strong>. Con <c>FK_CURSANTE_ALUMNO</c> en <c>Restrict</c>, el borrado de un
	/// alumno con una sola inscripción no conservaba nada: fallaba contra la clave ajena y llegaba
	/// como <c>DbUpdateException</c>. Ahora el borrado sí cascadea —lo orquesta
	/// <c>EliminarAlumnoAsync</c> en una transacción— y el diálogo informa las cifras exactas que
	/// devuelve <c>ConsultarImpactoEliminacionAsync</c> antes de tocar nada.
	/// </para>
	/// <para>
	/// La consulta de impacto va <strong>antes</strong> del diálogo y en su propio <c>try</c>: si
	/// falla, no hay que preguntar nada porque no se va a poder borrar, y el usuario tiene que
	/// enterarse de eso y no de un cartel genérico después de haber confirmado.
	/// </para>
	/// </remarks>
	private async Task ExecuteEliminarCommand()
	{
		string nombre = AlumnoSeleccionado.NombreCompleto;
		string documento = AlumnoSeleccionado.Documento;
		var request = new EliminarAlumnoRequest(AlumnoSeleccionado.PersonaID);

		ImpactoEliminacionAlumnoResponse impacto;
		try
		{
			impacto = await _servicioAlumnos.ConsultarImpactoEliminacionAsync(request);
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo averiguar qué se perdería");
			return;
		}

		// El verbo del diálogo es el del botón que lo abrió: la pantalla dice "Eliminar" y el
		// caso de uso es EliminarAlumno, así que acá no aparece un "Quitar" que no se usó.
		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Eliminar a { nombre } de la institución",
				consecuencia: DescribirConsecuencia(impacto, documento),
				verbo: "Eliminar alumno")))
		{
			return;
		}

		try
		{
			await _servicioAlumnos.EliminarAlumnoAsync(request);

			// La fila sale del índice y la ficha se vacía: dejar el alumno borrado en la lista es
			// la forma más rápida de que alguien opere sobre un registro que ya no existe. El
			// término se conserva, porque la búsqueda que lo encontró sigue siendo la que importa.
			var borrado = AlumnoSeleccionado;
			AlumnoSeleccionado = null;
			Resultados.Remove(borrado);

			Conteo = Resultados.Count is 0
				? string.Empty
				: Contar(Resultados.Count, "coincidencia", "coincidencias");

			Message = $"{ nombre } ya no figura en la institución. Buscá otro alumno para seguir.";

			_dialogService.MostrarInformacion($"{ nombre } ya no figura en la institución.", "Alumno eliminado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo quitar el alumno");
		}
	}

	/// <summary>
	/// El texto de la consecuencia, con las cifras exactas de lo que el borrado destruye.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es <c>static</c> y no toca estado del ViewModel a propósito: es la parte que vale la pena
	/// probar —la redacción y la pluralización cambian según los números— y así se ejercita sin
	/// construir el ViewModel con sus cinco dependencias.
	/// </para>
	/// <para>
	/// Dice las cifras <strong>antes</strong> de hablar del legajo porque es el orden en que importan:
	/// que se pierda el domicilio es menor al lado de que se pierdan 312 registros de asistencia. Un
	/// alumno sin trayectoria se lleva una sola frase, sin enumerar ceros.
	/// </para>
	/// </remarks>
	internal static string DescribirConsecuencia(ImpactoEliminacionAlumnoResponse impacto, string documento)
	{
		string legajo = $"El legajo del documento { documento } deja de estar disponible, junto con su "
					  + "domicilio y su contacto.";

		if (!impacto.TieneHistorial)
		{
			return $"{ legajo } No tiene inscripciones ni calificaciones registradas. "
				 + "Esta acción no se puede deshacer.";
		}

		var partes = new List<string>();

		if (impacto.Inscripciones > 0)
		{
			partes.Add(Contar(impacto.Inscripciones, "inscripción", "inscripciones"));
		}

		if (impacto.Calificaciones > 0)
		{
			partes.Add(Contar(impacto.Calificaciones, "calificación", "calificaciones"));
		}

		if (impacto.RegistrosAsistencia > 0)
		{
			partes.Add(Contar(impacto.RegistrosAsistencia, "registro de asistencia", "registros de asistencia"));
		}

		string ciclos = impacto.CiclosLectivos.Count switch
		{
			0 => string.Empty,
			1 => $" del ciclo { impacto.CiclosLectivos.First() }",
			_ => $" de los ciclos { Enumerar(impacto.CiclosLectivos) }"
		};

		string vaciadas = impacto.PlanillasQueQuedanVacias > 0
			? " " + Contar(impacto.PlanillasQueQuedanVacias, "planilla de asistencia queda", "planillas de asistencia quedan")
				  + " sin ningún registro y también se elimina"
				  + (impacto.PlanillasQueQuedanVacias is 1 ? "." : "n.")
			: string.Empty;

		return $"Se borran { Enumerar(partes) }{ ciclos }.{ vaciadas } { legajo } "
			 + "Esta acción no se puede deshacer.";
	}

	private static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";

	/// <summary>Une con comas y una "y" antes del último, como se escribe en castellano.</summary>
	private static string Enumerar(IReadOnlyCollection<string> partes) =>
		partes.Count switch
		{
			0 => string.Empty,
			1 => partes.First(),
			_ => $"{ string.Join(", ", partes.Take(partes.Count - 1)) } y { partes.Last() }"
		};
	#endregion
}
