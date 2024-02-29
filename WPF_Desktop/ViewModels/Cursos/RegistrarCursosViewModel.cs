using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class RegistrarCursosViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioCursos _servicioCursos;

    #region Request
    private CrearCursoRequest _crearCursoRequest;
    #endregion

    private int _curso;
    private ComboBoxItem _cursoSelected;
    private int _nivelEducativo = 0;
    private string _nivelEducativoSelected;

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand RegistrarCursoCommand { get; }
    #endregion


    public RegistrarCursosViewModel(IServicioCursos servicioCursos)
    {
        _servicioCursos = servicioCursos;

        RegistrarCursoCommand = new ViewModelCommand(ExecuteRegistrarCursoCommand, CanExecuteRegistrarCursoCommand);

        Curso = 0;
        NivelEducativo = 0;
    }

    #region Properties
    public int Curso
    {
        get
        {
            return _curso;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Curso));
            _curso = value;
            OnPropertyChanged(nameof(Curso));

            if (Curso < 0)
            {
                _errorsByProperty.Add(nameof(Curso), new List<string>
                {
                    "Se debe seleccionar el curso que desea seleccionar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Curso)));
            }

            /*if (string.IsNullOrWhiteSpace(Descripcion))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>
                {
                    "Se debe ingresar el nombre del curso que desea agregar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
            }

            if (!Regex.IsMatch(Descripcion.Trim(), @"^(\d){1}$", RegexOptions.None, TimeSpan.FromMilliseconds(2000)))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>
                {
                    "Se debe ingresar un dígito como nombre del curso que desea agregar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
            }*/
        }
    }

    public ComboBoxItem CursoSelected
    {
        get
        {
            return _cursoSelected;
        }

        set
        {
            _cursoSelected = value;
            OnPropertyChanged(nameof(CursoSelected));
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

            if (Curso < 0)
            {
                _errorsByProperty.Add(nameof(NivelEducativo), new List<string>
                {
                    "Se debe seleccionar el nivel educativo que desea seleccionar."
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

    #region RegistrarCursoCommand
    private bool CanExecuteRegistrarCursoCommand(object obj)
    {
        return !HasErrors;
    }

    private void ExecuteRegistrarCursoCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        messageBoxText = $"Se van a registrar los siguientes datos del nuevo curso:\n" +
                         $"Nombre: { CursoSelected.Content }\n" +
                         $"Nivel Educativo: { NivelEducativoSelected }\n\n" +
                         $"¿Desea continuar?";
        caption = "Registrar Curso";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result is MessageBoxResult.Yes)
        {
            try
            {
                _crearCursoRequest = new CrearCursoRequest((Curso + 1).ToString(), NivelEducativo);
                _servicioCursos.RegistrarCurso(_crearCursoRequest);
                MessageBox.Show("Datos guardados correctamente", "Operación exitosa",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                messageBoxText = $"Error al registrar un nuevo curso. { ex.Message }";
                caption = "Registrar curso";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion
}