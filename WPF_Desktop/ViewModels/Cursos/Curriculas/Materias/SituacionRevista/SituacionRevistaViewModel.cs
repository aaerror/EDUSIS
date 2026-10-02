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


	public SituacionRevistaViewModel(SituacionRevistaResponse situacionRevista)
	{
		/*FechaAlta = DateTime.Now;
		EnFunciones = true;*/

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
		}
	}

	public SituacionRevistaViewModel(Guid docenteID, string docente)
	{
		DocenteID = docenteID;
		Docente = docente;
		EnFunciones = true;
	}

	public DateTime FechaInicio
	{
		get
		{
			return _fechaInicio;
		}

		set
		{
			SetProperty(ref _fechaInicio, value, nameof(FechaInicio));

			EnFunciones = FechaInicio.Equals(DateTime.Today) ? true : false;
			if (!string.IsNullOrWhiteSpace(Cargo) && (Cargo.Equals("Suplente") || Cargo.Equals("Interino")))
			{
				FechaFin = FechaInicio;
				ValidateProperty(FechaFin, nameof(FechaFin));
			}
		}
	}

	partial void OnCargoChanged(string value)
	{
		if (string.Equals("Titular", value))
		{
			FechaFin = null;
		}

		if (string.Equals("Suplente", value) || string.Equals("Interino", value))
		{
			FechaFin = FechaInicio;
		}
	}
}