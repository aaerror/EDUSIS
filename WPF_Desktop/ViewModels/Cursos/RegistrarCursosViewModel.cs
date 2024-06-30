using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class RegistrarCursosViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioCurso _servicioCursos;

    #region Request
    private RegistrarCursoRequest _crearCursoRequest;
    #endregion

    private int _grado;
    private string _cursoSelected;
    private int _nivelEducativo;
    private string _nivelEducativoSelected;

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand RegistrarCommand { get; }
    #endregion


    public RegistrarCursosViewModel(IServicioCurso servicioCursos)
    {
        _servicioCursos = servicioCursos;

        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);

        Grado = 0;
        NivelEducativo = 0;
    }

    #region Properties
    public int Grado
    {
        get
        {
            return _grado;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Grado));
            _grado = value;
            OnPropertyChanged(nameof(Grado));

            if (Grado is -1)
            {
                _errorsByProperty.Add(nameof(Grado), new List<string>
                {
                    "Se debe seleccionar el grado del curso que desea registrar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Grado)));
            }
        }
    }

    public string GradoSelected
    {
        get
        {
            return _cursoSelected;
        }

        set
        {
            _cursoSelected = value;
            OnPropertyChanged(nameof(GradoSelected));
        }
    }

    public int NivelEducativo
    {
        get
        {
            return _nivelEducativo;
        }

        set
        {
            _errorsByProperty.Remove(nameof(NivelEducativo));
            _nivelEducativo = value;
            OnPropertyChanged(nameof(NivelEducativo));

            if (Grado is -1)
            {
                _errorsByProperty.Add(nameof(NivelEducativo), new List<string>
                {
                    "Se debe seleccionar el nivel educativo del curso que desea agregar."
                });
            }
        }
    }

    public string NivelEducativoSelected
    {
        get
        {
            return _nivelEducativoSelected;
        }

        set
        {
            _nivelEducativoSelected = value;
            OnPropertyChanged(nameof(NivelEducativoSelected));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        return !HasErrors;
    }

    private async void ExecuteRegistrarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        messageBoxText = $"Se van a registrar los siguientes datos del nuevo curso:\n" +
                         $"Grado: { GradoSelected }\n" +
                         $"Nivel Educativo: { NivelEducativoSelected }\n\n" +
                         $"¿Desea continuar?";
        caption = "Registrar Curso";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result is MessageBoxResult.Yes)
        {
            try
            {
                _crearCursoRequest = new RegistrarCursoRequest(Grado + 1, NivelEducativo);
                await _servicioCursos.RegistrarCurso(_crearCursoRequest);

                MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar un nuevo curso.{ ex.Message }", "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion
}