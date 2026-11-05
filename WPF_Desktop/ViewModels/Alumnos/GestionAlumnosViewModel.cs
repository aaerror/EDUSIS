using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Response;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

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

	[Required(AllowEmptyStrings=true, ErrorMessage="Se debe ingresar el documento del alumno.")]
	[RegularExpression(@"^\d{8}$", ErrorMessage="El documento debe ser un número.", MatchTimeoutInMilliseconds=2000)]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(BuscarCommand))]
	[ObservableProperty]
	private string _documento;

	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private PersonaResponse _personaResponse;

	[ObservableProperty]
	private string _message = string.Empty;

	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _existeAlumno;

	#region COMMANDs
	public IAsyncRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IAsyncRelayCommand BuscarCommand { get; }
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
		EliminarCommand = new AsyncRelayCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		Message = "Buscá un alumno por su documento para ver su perfil, inscribirlo o quitarlo. Si todavía no está cargado, usá «Registrar alumno».";
		ExisteAlumno = false;
	}

	#region BuscarCommand
	private bool CanExecuteBuscarCommand() =>
		!string.IsNullOrWhiteSpace(Documento) && !HasErrors;

	/// <summary>
	/// Busca el alumno por documento exacto.
	/// </summary>
	/// <remarks>
	/// Era <c>async void</c> sobre un <c>RelayCommand</c>: cualquier excepción que no cayera en
	/// el try —por ejemplo al escribir en el store— no la podía atrapar nadie y terminaba en el
	/// manejador global, que apaga la aplicación. Como <c>AsyncRelayCommand</c> además expone
	/// <c>IsRunning</c>, la pantalla puede mostrar que está buscando.
	/// </remarks>
	private async Task ExecuteBuscarCommand()
	{
		try
		{
			var response = await _servicioAlumnos.BuscarPorDNIAsync(new DocumentoRequest(Documento));
			if (response is null)
			{
				ExisteAlumno = false;
				Message = $"No hay ningún alumno con el documento { Documento }. Revisá el número o registralo con «Registrar alumno».";

				return;
			}

			PersonaResponse = response;
			_perfilStore.PersonaID = PersonaResponse.PersonaID;
			_perfilStore.Documento = PersonaResponse.Documento;

			ExisteAlumno = true;
			Message = string.Empty;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo buscar el alumno");
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Asignar" => PersonaResponse is not null,
		"Perfil" => PersonaResponse is not null,
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
		PersonaResponse is not null;

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
		string nombre = $"{ PersonaResponse.Apellido }, { PersonaResponse.Nombre }";
		var request = new EliminarAlumnoRequest(PersonaResponse.PersonaID);

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
				consecuencia: DescribirConsecuencia(impacto, PersonaResponse.Documento),
				verbo: "Eliminar alumno")))
		{
			return;
		}

		try
		{
			await _servicioAlumnos.EliminarAlumnoAsync(request);

			// La pantalla vuelve a su estado inicial: dejar la ficha de un alumno borrado es
			// la forma más rápida de que alguien opere sobre un registro que ya no existe.
			PersonaResponse = null;
			ExisteAlumno = false;
			Documento = string.Empty;
			Message = $"{ nombre } ya no figura en la institución. Buscá otro documento para seguir.";

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
