using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCatedras.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

internal class HorarioViewModel : ObservableObject
{
	private readonly HorarioResponse _horarioResponse = null;
	private bool _editarHorario = false;

	private string _turno;
	private string _horaInicio = string.Empty;
	private string _horaFin = string.Empty;
	private string _dia;
	private TimeOnly _horaInicioValor;
	private int _duracionMinutos;


	public HorarioViewModel(HorarioResponse horarioResponse)
	{
		EditarHorario = false;

		if (horarioResponse is not null)
		{
			_horarioResponse = horarioResponse;

			Dia = horarioResponse.Dia;
			Turno = horarioResponse.Turno;
			HoraInicio = horarioResponse.HoraInicio.ToString();
			HoraFin = horarioResponse.HoraFin.ToString();

			// Los dos valores crudos que la grilla NO muestra. Ver el comentario de las propiedades.
			HoraInicioValor = _horarioResponse.HoraInicio;
			DuracionMinutos = (int) (_horarioResponse.HoraFin - _horarioResponse.HoraInicio).TotalMinutes;
		}
	}

	#region Properties
	public bool EditarHorario
	{
		get
		{
			return _editarHorario;
		}

		set
		{
			_editarHorario = value;
			OnPropertyChanged(nameof(EditarHorario));
		}
	}

	public string Dia
	{
		get
		{
			return _dia;
		}

		set
		{
			_dia = value;
			OnPropertyChanged(nameof(Dia));
		}
	}

	public string HoraInicio
	{
		get
		{
			return _horaInicio;
		}

		set
		{
			_horaInicio = value;
			OnPropertyChanged(nameof(HoraInicio));
		}
	}

	public string HoraFin
	{
		get
		{
			return _horaFin;
		}

		set
		{
			_horaFin = value;
			OnPropertyChanged(nameof(HoraFin));
		}
	}

	public string Turno
	{
		get
		{
			return _turno;
		}

		set
		{
			_turno = value;
			OnPropertyChanged(nameof(Turno));
		}
	}

	/*
		POR QUÉ EXISTEN ESTAS DOS PROPIEDADES.

		La grilla muestra HoraInicio y HoraFin como texto, que es lo único que necesita para
		dibujarse. Pero para QUITAR un horario hay que reconstruirlo en el dominio, y
		QuitarHorarioRequest no pide un rango: pide el TimeOnly de inicio y la duración de la
		hora cátedra en minutos. Ninguno de los dos se puede recuperar de las propiedades string
		sin volver a parsear lo que ya se formateó, así que se guardan tal como vinieron en el
		HorarioResponse.

		La duración se calcula con TotalMinutes y NO con Minutes: Minutes es la componente de
		minutos del TimeSpan, así que una hora cátedra de SESENTA minutos —TimeSpan de 01:00:00—
		da 0. Es exactamente el defecto que arregló AS-2 en Domain/Catedras/Horarios/Horario.cs,
		y con un 0 acá el QuitarHorarioRequest no identifica ningún horario y la fila nunca
		desaparece de la grilla.
	*/
	public TimeOnly HoraInicioValor
	{
		get
		{
			return _horaInicioValor;
		}

		set
		{
			_horaInicioValor = value;
			OnPropertyChanged(nameof(HoraInicioValor));
		}
	}

	public int DuracionMinutos
	{
		get
		{
			return _duracionMinutos;
		}

		set
		{
			_duracionMinutos = value;
			OnPropertyChanged(nameof(DuracionMinutos));
		}
	}
	#endregion
}