using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioDocumentos;
using Core.ServicioDocumentos.DTOs.Requests;
using Core.Shared.Documentos;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Documentacion;

/// <summary>
/// Emisión de la documentación de un alumno: se busca por documento, se fija el ciclo lectivo y
/// se emite el certificado o la constancia que haga falta.
/// </summary>
/// <remarks>
/// La búsqueda es por documento exacto porque es lo único que expone <c>IServicioAlumno</c>
/// —<c>BuscarPorDNIAsync</c> devuelve UNA persona, no una colección—, igual que en la pantalla de
/// Alumnos. Mientras eso siga así, no hay listado para elegir.
/// </remarks>
internal partial class GestionDocumentacionViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioAlumno _servicioAlumno;
	private readonly IServicioDocumento _servicioDocumento;
	private readonly IArchivoService _archivos;
	private readonly IDialogService _dialogos;
	#endregion

	#region Stores
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe ingresar el documento del alumno.")]
	[RegularExpression(@"^\d{7,8}$", ErrorMessage = "El documento debe tener 7 u 8 dígitos, sin puntos.", MatchTimeoutInMilliseconds = 2000)]
	[NotifyCanExecuteChangedFor(nameof(BuscarAlumnoCommandAsync))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _documento = string.Empty;

	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe ingresar el ciclo lectivo.")]
	[RegularExpression(@"^\d{4}$", ErrorMessage = "El ciclo lectivo son cuatro dígitos.", MatchTimeoutInMilliseconds = 2000)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _cicloLectivo = string.Empty;

	/// <summary>Motivo del trámite. Opcional: si queda vacío, el documento no declara finalidad.</summary>
	[ObservableProperty]
	private string _finalidad = string.Empty;

	[ObservableProperty]
	private DateTime _desde = new DateTime(DateTime.Today.Year, 3, 1);

	[ObservableProperty]
	private DateTime _hasta = DateTime.Today;

	[NotifyCanExecuteChangedFor(nameof(CertificadoAlumnoRegularCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ConstanciaEscolaridadCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CertificadoAsistenciaCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasAprobadasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(BoletinCommandAsync))]
	[ObservableProperty]
	private PersonaResponse? _alumno;

	/// <summary>Resultado de la última búsqueda, para que la pantalla no quede en blanco sin explicación.</summary>
	[ObservableProperty]
	private string _mensaje = "Buscá un alumno por su documento para emitir su documentación.";

	public bool HayAlumno => Alumno is not null;

	#region AsyncCommands
	public IAsyncRelayCommand BuscarAlumnoCommandAsync { get; }
	public IAsyncRelayCommand CertificadoAlumnoRegularCommandAsync { get; }
	public IAsyncRelayCommand ConstanciaEscolaridadCommandAsync { get; }
	public IAsyncRelayCommand CertificadoAsistenciaCommandAsync { get; }
	public IAsyncRelayCommand MateriasAprobadasCommandAsync { get; }
	public IAsyncRelayCommand MateriasPendientesCommandAsync { get; }
	public IAsyncRelayCommand BoletinCommandAsync { get; }
	#endregion


	public GestionDocumentacionViewModel(IServicioAlumno servicioAlumno,
										 IServicioDocumento servicioDocumento,
										 IArchivoService archivos,
										 IDialogService dialogos,
										 CicloLectivoStore cicloLectivoStore)
	{
		_servicioAlumno = servicioAlumno;
		_servicioDocumento = servicioDocumento;
		_archivos = archivos;
		_dialogos = dialogos;
		_cicloLectivoStore = cicloLectivoStore;

		BuscarAlumnoCommandAsync = new AsyncRelayCommand(BuscarAlumnoAsync, CanExecuteBuscarAlumno);

		CertificadoAlumnoRegularCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioDocumento.GenerarCertificadoAlumnoRegularAsync(request)), CanExecuteEmitir);
		ConstanciaEscolaridadCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioDocumento.GenerarConstanciaEscolaridadAsync(request)), CanExecuteEmitir);
		MateriasAprobadasCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioDocumento.GenerarConstanciaMateriasAprobadasAsync(request)), CanExecuteEmitir);
		MateriasPendientesCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioDocumento.GenerarConstanciaMateriasPendientesAsync(request)), CanExecuteEmitir);
		BoletinCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioDocumento.GenerarBoletinCalificacionesAsync(request)), CanExecuteEmitir);

		CertificadoAsistenciaCommandAsync = new AsyncRelayCommand(EmitirCertificadoAsistenciaAsync, CanExecuteEmitir);

		// Después de crear los comandos: el setter notifica a BuscarAlumnoCommandAsync y el campo
		// arranca vacío, así que la asignación siempre entra al bloque de cambio.
		CicloLectivo = _cicloLectivoStore.CicloLectivo;
	}

	#region Cambios de propiedad
	partial void OnCicloLectivoChanged(string value)
	{
		// El formato de cuatro dígitos lo valida el dominio; acá sólo se evita guardar un vacío.
		if (!string.IsNullOrWhiteSpace(value))
		{
			_cicloLectivoStore.CicloLectivo = value;
		}
	}

	partial void OnAlumnoChanged(PersonaResponse? value)
	{
		OnPropertyChanged(nameof(HayAlumno));
	}
	#endregion

	#region Búsqueda
	private bool CanExecuteBuscarAlumno() =>
		!string.IsNullOrWhiteSpace(Documento) && !GetErrors(nameof(Documento)).Any();

	private async Task BuscarAlumnoAsync()
	{
		try
		{
			var encontrado = await _servicioAlumno.BuscarPorDNIAsync(new DocumentoRequest(Documento));

			Alumno = encontrado;
			Mensaje = encontrado is null
				? $"No hay ningún alumno con documento { Documento }. El documento tiene que coincidir exacto."
				: $"{ encontrado.Apellido }, { encontrado.Nombre } · DNI { encontrado.Documento }";
		}
		catch (Exception ex)
		{
			Alumno = null;
			_dialogos.MostrarError(ex.Message, "Error al buscar el alumno");
		}
	}
	#endregion

	#region Emisión
	private bool CanExecuteEmitir() =>
		HayAlumno && !string.IsNullOrWhiteSpace(CicloLectivo) && !GetErrors(nameof(CicloLectivo)).Any();

	/// <summary>
	/// Camino común de los cinco documentos que sólo necesitan alumno, ciclo lectivo y finalidad.
	/// </summary>
	private async Task EmitirAsync(Func<EmitirDocumentoAlumnoRequest, Task<DocumentoGenerado>> emitir)
	{
		if (Alumno is null)
		{
			return;
		}

		var request = new EmitirDocumentoAlumnoRequest(Alumno.PersonaID, CicloLectivo, Finalidad);

		await EntregarAsync(() => emitir(request));
	}

	private async Task EmitirCertificadoAsistenciaAsync()
	{
		if (Alumno is null)
		{
			return;
		}

		if (Desde.Date > Hasta.Date)
		{
			_dialogos.MostrarAdvertencia("El inicio del período es posterior a su fin.", "Período inválido");
			return;
		}

		var request = new EmitirCertificadoAsistenciaRequest(Alumno.PersonaID, CicloLectivo, Desde, Hasta, Finalidad);

		await EntregarAsync(() => _servicioDocumento.GenerarCertificadoAsistenciaAsync(request));
	}

	/// <summary>
	/// Genera, pide la ruta y abre. Un cancelar en el cuadro de guardado no es un error: no se
	/// avisa nada y el documento generado se descarta.
	/// </summary>
	private async Task EntregarAsync(Func<Task<DocumentoGenerado>> generar)
	{
		try
		{
			var documento = await generar();
			var ruta = _archivos.GuardarYAbrir(documento);

			if (ruta is not null)
			{
				Mensaje = $"Documento guardado en { ruta }";
			}
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al emitir el documento");
		}
	}
	#endregion
}
