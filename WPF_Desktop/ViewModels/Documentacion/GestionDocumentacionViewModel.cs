using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioDocumentos;
using Core.ServicioDocumentos.DTOs.Requests;
using Core.Shared.Documentos;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// <para>
/// El ciclo lectivo se <b>elige</b>, no se tipea: la lista sale del catálogo <c>ciclo_lectivo</c>
/// por <c>IServicioDocumento.ListarCiclosLectivosAsync</c>. Antes era un campo libre de cuatro
/// dígitos y se podía pedir documentación de un año que la base no habilita, que recién fallaba
/// al emitir.
/// </para>
/// <para>
/// La búsqueda es por documento exacto porque es lo único que expone <c>IServicioAlumno</c>
/// —<c>BuscarPorDNIAsync</c> devuelve UNA persona, no una colección—, igual que en la pantalla de
/// Alumnos. Mientras eso siga así, no hay listado para elegir.
/// </para>
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

	private const string SIN_ALUMNO = "Buscá un alumno por su documento para emitir su documentación.";

	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe ingresar el documento del alumno.")]
	[RegularExpression(@"^\d{7,8}$", ErrorMessage = "El documento debe tener 7 u 8 dígitos, sin puntos.", MatchTimeoutInMilliseconds = 2000)]
	[NotifyCanExecuteChangedFor(nameof(BuscarAlumnoCommandAsync))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _documento = string.Empty;

	[ObservableProperty]
	private ObservableCollection<string> _ciclosLectivos = new();

	// La validación de formato que había acá —cuatro dígitos— se fue con el campo libre: ahora el
	// valor sale del catálogo de la base y lo único que puede faltar es la elección.
	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe elegir el ciclo lectivo.")]
	[NotifyCanExecuteChangedFor(nameof(CertificadoAlumnoRegularCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ConstanciaEscolaridadCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CertificadoAsistenciaCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasAprobadasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(BoletinCommandAsync))]
	[NotifyPropertyChangedFor(nameof(Alcance))]
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
	[NotifyPropertyChangedFor(nameof(Alcance))]
	[ObservableProperty]
	private PersonaResponse? _alumno;

	/// <summary>
	/// Hay una emisión en curso. Apaga los seis botones mientras tanto, para que la pantalla no
	/// quede muda mientras se arma el PDF y no se termine apretando dos veces.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(CertificadoAlumnoRegularCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ConstanciaEscolaridadCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CertificadoAsistenciaCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasAprobadasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(BoletinCommandAsync))]
	[ObservableProperty]
	private bool _emitiendo;

	/// <summary>Resultado de la última búsqueda, para que la pantalla no quede en blanco sin explicación.</summary>
	[ObservableProperty]
	private string _mensaje = SIN_ALUMNO;

	public bool HayAlumno => Alumno is not null;

	/// <summary>
	/// A nombre de quién y por qué período sale el documento, dicho con las mismas palabras que
	/// lleva impresas. Los seis documentos se emiten contra este par: sin él no hay nada que pedir.
	/// </summary>
	public string Alcance
	{
		get
		{
			if (Alumno is null)
			{
				return "Falta elegir el alumno";
			}

			var alumno = $"{ Alumno.Apellido }, { Alumno.Nombre } · DNI { Alumno.Documento }";

			return string.IsNullOrWhiteSpace(CicloLectivo)
				? $"{ alumno } · falta el ciclo lectivo"
				: $"{ alumno } · Ciclo { CicloLectivo }";
		}
	}

	#region AsyncCommands
	public IAsyncRelayCommand CargarCommandAsync { get; }
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

		CargarCommandAsync = new AsyncRelayCommand(CargarCiclosLectivosAsync);

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

		// El ciclo lectivo NO se asigna acá: el valor del store se elige recién cuando llegó el
		// catálogo, en CargarCiclosLectivosAsync. Un SelectedItem que no está en ItemsSource lo
		// descarta el propio ComboBox, así que asignarlo antes de tener la lista lo perdería.
	}

	#region Cambios de propiedad
	partial void OnCicloLectivoChanged(string value)
	{
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

	#region Carga
	private async Task CargarCiclosLectivosAsync()
	{
		try
		{
			var ciclos = await _servicioDocumento.ListarCiclosLectivosAsync();

			CiclosLectivos = new ObservableCollection<string>(ciclos);
			CicloLectivo = Preferir(ciclos);

			if (CiclosLectivos.Count == 0)
			{
				Mensaje = "La base no tiene ningún ciclo lectivo habilitado, así que no hay período contra el cual emitir.";
			}
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al cargar los ciclos lectivos");
		}
	}

	/// <summary>
	/// Qué período queda elegido al abrir: el que venía de otra pantalla, si no el año en curso, y
	/// si ninguno de los dos está habilitado, el más reciente del catálogo. El store arranca en el
	/// año en curso y nadie garantiza que la base lo tenga cargado.
	/// </summary>
	private string Preferir(IReadOnlyCollection<string> ciclos)
	{
		if (ciclos.Contains(_cicloLectivoStore.CicloLectivo))
		{
			return _cicloLectivoStore.CicloLectivo;
		}

		var añoEnCurso = DateTime.Today.Year.ToString();

		return ciclos.Contains(añoEnCurso)
			? añoEnCurso
			: ciclos.FirstOrDefault() ?? string.Empty;
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
				: $"Alumno encontrado. Elegí el documento que necesites emitir.";
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
		!Emitiendo && HayAlumno && !string.IsNullOrWhiteSpace(CicloLectivo) && !GetErrors(nameof(CicloLectivo)).Any();

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
			Emitiendo = true;

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
		finally
		{
			Emitiendo = false;
		}
	}
	#endregion
}
