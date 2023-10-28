using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class ProfesorViewModel : ViewModel
{
    private readonly SituacionRevistaProfesorResponse _situacionRevista = null;

    private Guid _profesor = Guid.Empty;
    private string _nombreCompleto = string.Empty;
    private string _cargo = string.Empty;


    public ProfesorViewModel(SituacionRevistaProfesorResponse situacionRevista)
    {
        if (situacionRevista != null)
        {
            _situacionRevista = situacionRevista;
            Profesor = _situacionRevista.Profesor;
            NombreCompleto = _situacionRevista.NombreCompleto;
            Cargo = _situacionRevista.Cargo;
        }
    }

    #region Properties
    public Guid Profesor
    {
        get
        {
            return _profesor;
        }

        set
        {
            _profesor = value;
            OnPropertyChanged(nameof(Profesor));
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

    public string Cargo
    {
        get
        {
            return _cargo;
        }

        set
        {
            _cargo = value;
            OnPropertyChanged(nameof(Cargo));
        }
    }
    #endregion
}
