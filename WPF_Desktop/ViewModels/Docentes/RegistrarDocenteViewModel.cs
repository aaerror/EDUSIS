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

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new RelayCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
		GuardarCommand = new AsyncRelayCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
	}

	#region GuardarCommand
	private bool CanExecuteGuardarCommand() =>
		!HasErrors && !InformacionPersonal.HasErrors && !Domicilio.HasErrors && !Contacto.HasErrors;

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
	/// </remarks>
	private async Task ExecuteGuardarCommand()
	{
		if (string.IsNullOrWhiteSpace(LegajoDocente.Legajo) || string.IsNullOrWhiteSpace(LegajoDocente.PrefijoCuil) || string.IsNullOrWhiteSpace(LegajoDocente.PosfijoCuil))
		{
			_dialogService.MostrarAdvertencia("Completá el legajo y las dos partes del CUIL para dar de alta el docente.", "Faltan datos del legajo");

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
	private bool CanExecuteContinuarCommand() =>
		!InformacionPersonal.HasErrors && !Domicilio.HasErrors && !Contacto.HasErrors;

	/// <summary>
	/// Pasa al segundo paso del alta.
	/// </summary>
	/// <remarks>
	/// Acá estaba el peor defecto del formulario: si faltaba UN campo, la pantalla reemplazaba
	/// los tres ViewModels del paso por instancias vacías. Es decir, avisaba que faltaba un dato
	/// y a cambio borraba todos los demás —nombre, documento, domicilio y contacto—. El usuario
	/// volvía a empezar de cero por haberse salteado el teléfono.
	///
	/// Ahora el aviso nombra lo que falta y lo cargado queda donde está.
	/// </remarks>
	private void ExecuteContinuarCommand()
	{
		var faltantes = new List<string>();

		if (string.IsNullOrWhiteSpace(InformacionPersonal.Apellido)) { faltantes.Add("apellido"); }
		if (string.IsNullOrWhiteSpace(InformacionPersonal.Nombre)) { faltantes.Add("nombre"); }
		if (string.IsNullOrWhiteSpace(InformacionPersonal.DocumentoNacionalIdentidad)) { faltantes.Add("documento"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Calle)) { faltantes.Add("calle"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Localidad)) { faltantes.Add("localidad"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Provincia)) { faltantes.Add("provincia"); }
		if (string.IsNullOrWhiteSpace(Contacto.Telefono)) { faltantes.Add("teléfono"); }
		if (string.IsNullOrWhiteSpace(Contacto.Email)) { faltantes.Add("correo"); }

		if (faltantes.Count > 0)
		{
			_dialogService.MostrarAdvertencia($"Falta completar: { string.Join(", ", faltantes) }.", "Datos incompletos");

			return;
		}

		LegajoDocente.NombreCompleto = InformacionPersonal.Apellido + ", " + InformacionPersonal.Nombre;
		LegajoDocente.DocumentoNacionalIdentidad = InformacionPersonal.DocumentoNacionalIdentidad;
		Tab = 1;
	}
	#endregion

	#region AtrasCommand
	private bool CanExecuteAtrasCommand() =>
		!LegajoDocente.HasErrors && !PuestoDocente.HasErrors;

	private void ExecuteAtrasCommand() =>
		Tab = 0;
	#endregion
}