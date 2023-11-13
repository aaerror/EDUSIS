/*using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class ProfesorViewModel : ViewModel
{
    private readonly SituacionRevistaProfesorResponse _situacionRevista = null;

    private Guid _docente = Guid.Empty;
    private string _nombreCompleto = string.Empty;
    private SituacionRevistaViewModel _situacionRevistaViewModel;


    public ProfesorViewModel(SituacionRevistaProfesorResponse situacionRevista)
    {
        if (situacionRevista != null)
        {
            _situacionRevista = situacionRevista;
            Docente = _situacionRevista.DocenteID;
            NombreCompleto = _situacionRevista.NombreCompleto;
            Cargo = _situacionRevista.Cargo;
        }
    }

    #region Properties
    public Guid Docente
    {
        get
        {
            return _docente;
        }

        set
        {
            _docente = value;
            OnPropertyChanged(nameof(Docente));
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

    public int Cargo
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
*/