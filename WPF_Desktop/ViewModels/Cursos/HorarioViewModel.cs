using Core.ServicioCursos.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class HorarioViewModel : ViewModel
{
    private readonly HorarioResponse _horarioResponse = null;
    private bool _editarHorario = false;

    private string _turno;
    private string _horaInicio = string.Empty;
    private string _horaFin = string.Empty;
    private string _diaSemana;


    public HorarioViewModel(HorarioResponse horarioResponse)
    {
        EditarHorario = false;

        if (horarioResponse is not null)
        {
            _horarioResponse = horarioResponse;

            DiaSemana = horarioResponse.DiaSemana;
            Turno = horarioResponse.Turno;
            HoraInicio = horarioResponse.HoraInicio;
            HoraFin = horarioResponse.HoraFin;
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

    public string DiaSemana
    {
        get
        {
            return _diaSemana;
        }

        set
        {
            _diaSemana = value;
            OnPropertyChanged(nameof(DiaSemana));
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
