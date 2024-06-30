using Core.ServicioMaterias.DTOs.Responses;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

public class HorarioViewModel : ViewModel
{
    private readonly HorarioResponse _horarioResponse = null;
    private bool _editarHorario = false;

    private string _turno;
    private string _horaInicio = string.Empty;
    private string _horaFin = string.Empty;
    private string _dia;


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
    #endregion
}
