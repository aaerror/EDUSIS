using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCatedras.DTOs.Responses;
using System;
using WPF_Desktop.Validations;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

internal partial class SituacionRevistaViewModel : ObservableValidator
{
	private readonly SituacionRevistaResponse _situacionRevista;

	[ObservableProperty]
	private Guid _situacionRevistaID = Guid.Empty;

	[ObservableProperty]
	private Guid _catedraID = Guid.Empty;

	[ObservableProperty]
	private Guid _docenteID = Guid.Empty;

	[ObservableProperty]
	private string _docente;

	[ObservableProperty]
	private string _cargo = string.Empty;

	private DateTime _fechaInicio = DateTime.Today;

	[DateAfterOrEqual(nameof(FechaInicio))]
	[ObservableProperty]
	private DateTime? _fechaFin;

	[ObservableProperty]
	private bool _enFunciones;

	[ObservableProperty]
	private Guid? _reemplazaA;

	[ObservableProperty]
	private string _estado = string.Empty;

	#region Estado calculado por el dominio
	/// <summary>
	/// La designación está vigente, según <c>SituacionRevista.EsCargoVigente()</c>.
	/// </summary>
	/// <remarks>
	/// No es lo mismo que <c>Estado != "Finalizado"</c>, que es como se lo venía deduciendo: una
	/// designación aceptada cuyo período ya terminó sigue diciendo "Aceptado" y no está vigente. De
	/// esa diferencia salían dos botones habilitados sobre filas que el dominio iba a rechazar.
	/// </remarks>
	[ObservableProperty]
	private bool _vigente;

	/// <summary>Titular o interino: ocupa el puesto. El suplente no lo ocupa, cubre una ausencia.</summary>
	[ObservableProperty]
	private bool _esOcupante;

	/// <summary>El docente al que esta suplencia reemplaza. <see cref="ReemplazaA"/> es la designación, no la persona.</summary>
	[ObservableProperty]
	private Guid? _reemplazaADocenteID;

	/// <summary>Nombre del docente reemplazado. Lo completa el contenedor, igual que <see cref="Docente"/>.</summary>
	[ObservableProperty]
	private string _reemplazaADocente = string.Empty;

	/// <summary>
	/// Esta designación ya tiene un suplente vigente cubriéndola.
	/// </summary>
	/// <remarks>
	/// Sale de <c>Catedra.TieneSuplenteVigente()</c>, la misma condición con la que <c>Designar</c>
	/// rechaza una segunda suplencia sobre el mismo ausente. La pantalla la necesita para no ofrecer
	/// en el combo "¿a quién reemplaza?" a alguien que el dominio va a rechazar.
	/// </remarks>
	[ObservableProperty]
	private bool _tieneSuplenteVigente;
	#endregion


	public SituacionRevistaViewModel(SituacionRevistaResponse situacionRevista)
	{
		if (situacionRevista is not null)
		{
			_situacionRevista = situacionRevista;

			SituacionRevistaID = _situacionRevista.SituacionRevistaID;
			CatedraID = _situacionRevista.CatedraID;
			DocenteID = _situacionRevista.DocenteID;
			Estado = _situacionRevista.Estado;
			Cargo = _situacionRevista.Cargo;
			FechaInicio = _situacionRevista.FechaInicio;
			FechaFin = _situacionRevista.FechaFin;
			ReemplazaA = _situacionRevista.ReemplazaA;
			EnFunciones = _situacionRevista.EnFunciones;
			Vigente = _situacionRevista.Vigente;
			EsOcupante = _situacionRevista.EsOcupante;
			ReemplazaADocenteID = _situacionRevista.ReemplazaADocenteID;
			TieneSuplenteVigente = _situacionRevista.TieneSuplenteVigente;
		}
	}

	/// <summary>Instancia de ALTA: el formulario de una designación que todavía no existe.</summary>
	/// <remarks>
	/// Ya no nace con <c>EnFunciones = true</c>. Poner al docente al frente del curso es una decisión
	/// aparte de designarlo —hay cátedras donde se designa hoy para que empiece el mes que viene— y la
	/// hacía sola, con la casilla que la reflejaba deshabilitada y sin mencionarla en la confirmación.
	/// Ahora el alta designa, y poner en aula es un paso que se elige.
	/// </remarks>
	public SituacionRevistaViewModel(Guid docenteID, string docente)
	{
		DocenteID = docenteID;
		Docente = docente;
	}

	/// <remarks>
	/// El setter ya no tiene efectos laterales. Hacía dos cosas a escondidas: prendía
	/// <c>EnFunciones</c> cuando la fecha era hoy, y le ponía a todo suplente o interino una
	/// <c>FechaFin</c> igual a la de inicio —una designación de UN DÍA— para esquivar
	/// <c>CargoTemporalSinFechaFinalizacionException</c>. Esquivaba la excepción guardando un dato
	/// falso; ahora la fecha de fin es un campo que el usuario completa y que el comando exige.
	/// </remarks>
	public DateTime FechaInicio
	{
		get
		{
			return _fechaInicio;
		}

		set
		{
			SetProperty(ref _fechaInicio, value, nameof(FechaInicio));
			ValidateProperty(FechaFin, nameof(FechaFin));
		}
	}

	/// <summary>Este cargo exige fecha de fin: el dominio rechaza un interino o un suplente sin ella.</summary>
	public bool ExigeFechaFin =>
		string.Equals("Suplente", Cargo) || string.Equals("Interino", Cargo);

	/// <summary>Este cargo reemplaza a alguien: sólo la suplencia.</summary>
	public bool ExigeReemplazo =>
		string.Equals("Suplente", Cargo);

	partial void OnCargoChanged(string value)
	{
		// El titular es por tiempo indeterminado: su fecha de fin se establece después, con
		// "Poner fin de designación". Para los otros dos el campo queda en blanco a propósito, para
		// que se note que falta en vez de nacer con un valor que nadie eligió.
		if (string.Equals("Titular", value))
		{
			FechaFin = null;
		}

		OnPropertyChanged(nameof(ExigeFechaFin));
		OnPropertyChanged(nameof(ExigeReemplazo));
	}
}