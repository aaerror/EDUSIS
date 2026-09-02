using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCurriculas.DTOs.Responses;
using System;
using WPF_Desktop.Validations;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

internal partial class SituacionRevistaViewModel : ObservableValidator
{
	private readonly SituacionRevistaResponse _situacionRevista;

	[ObservableProperty]
	private Guid _situacionRevistaID = Guid.Empty;

	[ObservableProperty]
	private Guid _materiaID = Guid.Empty;

	[ObservableProperty]
	private Guid _docenteID = Guid.Empty;

	[ObservableProperty]
	private string _docente;

	[ObservableProperty]
	private string _cargo = string.Empty;

	private DateTime _fechaAlta = DateTime.Today;

	[DateAfterOrEqual(nameof(FechaAlta))]
	[ObservableProperty]
	private DateTime? _fechaBaja;

	[ObservableProperty]
	private bool _enFunciones;

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
			MateriaID = _situacionRevista.MateriaID;
			DocenteID = _situacionRevista.DocenteID;
			Docente = _situacionRevista.Docente;
			Estado = _situacionRevista.Estado;
			Cargo = _situacionRevista.Cargo;
			FechaAlta = _situacionRevista.FechaAlta;
			FechaBaja = _situacionRevista.FechaBaja;
			EnFunciones = _situacionRevista.EnFunciones;
		}
	}

	public SituacionRevistaViewModel(Guid docenteID, string docente)
	{
		DocenteID = docenteID;
		Docente = docente;
		EnFunciones = true;
	}

	public DateTime FechaAlta
	{
		get
		{
			return _fechaAlta;
		}

		set
		{
			SetProperty(ref _fechaAlta, value, nameof(FechaAlta));

			EnFunciones = FechaAlta.Equals(DateTime.Today) ? true : false;
			if (!string.IsNullOrWhiteSpace(Cargo) && (Cargo.Equals("Suplente") || Cargo.Equals("Interino")))
			{
				FechaBaja = FechaAlta;
				ValidateProperty(FechaBaja, nameof(FechaBaja));
			}
		}
	}

	partial void OnCargoChanged(string value)
	{
		if (string.Equals("Titular", value))
		{
			FechaBaja = null;
		}

		if (string.Equals("Suplente", value) || string.Equals("Interino", value))
		{
			FechaBaja = FechaAlta;
		}
	}
}