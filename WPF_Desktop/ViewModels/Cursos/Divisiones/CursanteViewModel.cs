using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursantes.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal class CursanteViewModel : ObservableObject
{
	private CursanteResponse _cursanteResponse;

	private Guid _cursanteID;
	private Guid _alumnoID;
	private string _nombreCompleto;
	private string _documento;
	private int _edad;
	private bool _esRecursante;


	public CursanteViewModel(CursanteResponse cursanteResponse)
	{
		CursanteID = Guid.Empty;
		AlumnoID = Guid.Empty;
		NombreCompleto = string.Empty;
		Documento = string.Empty;
		Edad = 0;

		if (cursanteResponse is not null)
		{
			_cursanteResponse = cursanteResponse;
			CursanteID = _cursanteResponse.CursanteID;
			AlumnoID = _cursanteResponse.AlumnoID;
			NombreCompleto = _cursanteResponse.NombreCompleto;
			Documento = _cursanteResponse.Documento;
			Edad = _cursanteResponse.Edad;
			EsRecursante = _cursanteResponse.EsRecursante;
		}
	}

	#region Properties
	public Guid CursanteID
	{
		get
		{
			return _cursanteID;
		}

		set
		{
			_cursanteID = value;
			OnPropertyChanged(nameof(CursanteID));
		}
	}

	public Guid AlumnoID
	{
		get
		{
			return _alumnoID;
		}

		set
		{
			_alumnoID = value;
			OnPropertyChanged(nameof(AlumnoID));
		}
	}

	public string NombreCompleto
	{
		get
		{
			return _nombreCompleto;
		}

		set
		{
			_nombreCompleto = value;
			OnPropertyChanged(nameof(NombreCompleto));
		}
	}

	public string Documento
	{
		get
		{
			return _documento;
		}

		set
		{
			_documento = value;
			OnPropertyChanged(nameof(Documento));
		}
	}

	public int Edad
	{
		get
		{
			return _edad;
		}

		set
		{
			_edad = value;
			OnPropertyChanged(nameof(Edad));
		}
	}

	public bool EsRecursante
	{
		get
		{
			return _esRecursante;
		}

		set
		{
			_esRecursante = value;
			OnPropertyChanged(nameof(EsRecursante));
		}
	}
	#endregion
}