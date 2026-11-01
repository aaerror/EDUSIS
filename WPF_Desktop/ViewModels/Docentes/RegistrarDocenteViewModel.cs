using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Docentes.Puestos;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class RegistrarDocenteViewModel : ObservableValidator, IUbicacion
{
	private readonly IServicioDocente _servicioDocentes;
	private readonly IDialogService _dialogService;

	#region ViewModels
	[ObservableProperty]
	private InformacionPersonalViewModel _informacionPersonal;

	[ObservableProperty]
	private ContactoViewModel _contacto;

	[ObservableProperty]
	private DomicilioViewModel _domicilio;

	[ObservableProperty]
	private LegajoDocenteViewModel _legajoDocente;

	[ObservableProperty]
	private PuestoDocenteViewModel _puestoDocente;
	#endregion

	[ObservableProperty]
	private int _tab = 0;

	#region Commands
	public IAsyncRelayCommand GuardarCommand { get; }
	public IRelayCommand ContinuarCommand { get; }
	public IRelayCommand AtrasCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	public RegistrarDocenteViewModel(IServicioDocente servicioDocentes,
									 IDialogService dialogService,
									 INavigationService gestionDocentesNavigationService)
	{
		_servicioDocentes = servicioDocentes;
		_dialogService = dialogService;

		_informacionPersonal = new();
		_contacto = new(null);
		_domicilio = new(null);
		_legajoDocente = new(null);
		_puestoDocente = new();

		VolverCommand = new RelayCommand(gestionDocentesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("DOCENTES", VolverCommand),
			Migaja.Actual("ALTA")
		};

		/*
			LOS TRES COMANDOS IBAN CON UN CanExecute QUE NO SERVÍA PARA NADA. El RelayCommand de
			CommunityToolkit no escucha CommandManager.RequerySuggested —a diferencia del
			RoutedCommand de WPF—, así que su CanExecute se evalúa cuando se enlaza y no vuelve
			a evaluarse hasta que alguien llame NotifyCanExecuteChanged. Nadie llamaba, y los
			sub-ViewModels tampoco estaban suscritos: los tres botones se calculaban una vez,
			con los bloques recién creados y sin errores, y quedaban habilitados para siempre.

			No se cablean: se borran. Un botón de avanzar que se apaga solo deja al usuario sin
			nada que leer —no hay dónde decirle POR QUÉ no puede seguir—, y el de "Atrás" era
			peor, porque se apagaba con el legajo mal cargado y encerraba a quien quisiera
			volver a corregir el paso anterior. Se avanza siempre, se valida al avanzar, y el
			error aparece en el campo que lo tiene.
		*/
		AtrasCommand = new RelayCommand(ExecuteAtrasCommand);
		ContinuarCommand = new RelayCommand(ExecuteContinuarCommand);
		GuardarCommand = new AsyncRelayCommand(ExecuteGuardarCommand);
	}

	#region GuardarCommand
	/// <summary>
	/// Registra el docente con los datos de los dos pasos.
	/// </summary>
	/// <remarks>
	/// <para>
	/// DOS DEFECTOS QUE ESTABAN ACÁ. El primero: si faltaba el legajo o una parte del CUIL, la
	/// pantalla avisaba "Se deben ingresar los datos correspondientes" y después BORRABA los
	/// tres campos. Quien se había olvidado la mitad del CUIL perdía también la mitad que sí
	/// había escrito. Ahora el aviso dice qué falta y no toca lo cargado.
	/// </para>
	/// <para>
	/// El segundo: era <c>async void</c>, así que una excepción que se escapara del try subía
	/// al manejador global, que apaga la aplicación con un alta a medio cargar.
	/// </para>
	/// <para>
	/// Tampoco pregunta "¿Está seguro?" antes de guardar: el usuario recorrió dos pasos y
	/// apretó "Guardar". Lo que faltaba no era una pregunta más, era que los campos con error
	/// se marcaran mientras escribe (la vista ya no apaga el adorno de validación).
	/// </para>
	/// <para>
	/// El control de "faltan datos" era un IsNullOrWhiteSpace sobre tres campos que terminaba
	/// en un aviso al pie: decía que faltaba el legajo sin marcar el legajo, y no atrapaba el
	/// legajo MAL cargado, sólo el vacío. Ahora valida los dos bloques del paso y deja que
	/// cada campo muestre su propio motivo, que es donde el usuario está mirando.
	/// </para>
	/// </remarks>
	private async Task ExecuteGuardarCommand()
	{
		LegajoDocente.Validar();
		PuestoDocente.Validar();

		if (LegajoDocente.HasErrors || PuestoDocente.HasErrors)
		{
			return;
		}

		var requestDatosPersonales = new RegistrarDatosPersonalesRequest(
			Apellido: InformacionPersonal.Apellido,
			Nombre: InformacionPersonal.Nombre,
			Documento: InformacionPersonal.DocumentoNacionalIdentidad,
			Sexo: InformacionPersonal.Sexo.ToString(),
			FechaNacimiento: InformacionPersonal.FechaNacimiento,
			Nacionalidad: InformacionPersonal.Nacionalidad);

		var requestDomicilio = new RegistrarDomicilioRequest(
			Calle: Domicilio.Calle,
			Altura: Domicilio.Altura,
			Vivienda: Domicilio.Vivienda.ToString(),
			Observacion: Domicilio.Observaciones,
			Localidad: Domicilio.Localidad,
			Provincia: Domicilio.Provincia,
			Pais: Domicilio.Pais);

		var requestContacto = new RegistrarContactoRequest(
			Telefono: Contacto.Telefono,
			Email: Contacto.Email);

		var requestPuesto = new RegistrarPuestoDocenteRequest(
			DocenteID: Guid.Empty,
			Posicion: PuestoDocente.Posicion,
			Estado: PuestoDocente.Estado,
			FechaInicio: PuestoDocente.FechaInicio,
			FechaFin: PuestoDocente.FechaFin);

		var request = new RegistrarDocenteRequest(
			Legajo: LegajoDocente.Legajo,
			CUIL: LegajoDocente.CodigoUnicoIdentificacionLaboral,
			FechaAlta: LegajoDocente.FechaAlta,
			DatosPersonales: requestDatosPersonales,
			Domicilio: requestDomicilio,
			Contacto: requestContacto,
			Puesto: requestPuesto);

		string nombreCompleto = $"{ InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }";

		try
		{
			await _servicioDocentes.RegistrarDocenteAsync(request);

			InformacionPersonal = new InformacionPersonalViewModel(null);
			Domicilio = new DomicilioViewModel(null);
			Contacto = new ContactoViewModel(null);
			LegajoDocente = new LegajoDocenteViewModel(null);
			PuestoDocente = new PuestoDocenteViewModel(null);

			Tab = 0;

			_dialogService.MostrarInformacion($"{ nombreCompleto }, legajo { request.Legajo }. Ya figura en el listado de docentes.", "Docente registrado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo registrar el docente");
		}
	}
	#endregion

	#region ContinuarCommand
	/// <summary>
	/// Pasa al segundo paso del alta.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Acá estaba el peor defecto del formulario: si faltaba UN campo, la pantalla reemplazaba
	/// los tres ViewModels del paso por instancias vacías. Es decir, avisaba que faltaba un dato
	/// y a cambio borraba todos los demás —nombre, documento, domicilio y contacto—. El usuario
	/// volvía a empezar de cero por haberse salteado el teléfono.
	/// </para>
	/// <para>
	/// Después de eso quedó una lista de ocho IsNullOrWhiteSpace escrita a mano que terminaba
	/// en un aviso al pie: "Falta completar: apellido, documento, teléfono". El dato ya estaba
	/// dicho —cada campo tiene su [Required] en el ViewModel del bloque—, así que la lista era
	/// una segunda copia de las reglas que había que mantener al día, y además dejaba al
	/// usuario buscando por la pantalla cuáles de los catorce campos eran esos tres.
	/// </para>
	/// <para>
	/// Validar los tres bloques enteros marca cada campo donde está, con su propio motivo, y
	/// de paso atrapa lo que la lista no miraba: el documento de cinco dígitos, el correo sin
	/// arroba, el teléfono con letras. Por eso tampoco queda aviso al pie: sería repetir en un
	/// cartel lo que ya está escrito debajo de cada campo.
	/// </para>
	/// </remarks>
	private void ExecuteContinuarCommand()
	{
		InformacionPersonal.Validar();
		Domicilio.Validar();
		Contacto.Validar();

		if (InformacionPersonal.HasErrors || Domicilio.HasErrors || Contacto.HasErrors)
		{
			return;
		}

		LegajoDocente.NombreCompleto = InformacionPersonal.Apellido + ", " + InformacionPersonal.Nombre;
		LegajoDocente.DocumentoNacionalIdentidad = InformacionPersonal.DocumentoNacionalIdentidad;
		Tab = 1;
	}
	#endregion

	#region AtrasCommand
	private void ExecuteAtrasCommand() =>
		Tab = 0;
	#endregion
}