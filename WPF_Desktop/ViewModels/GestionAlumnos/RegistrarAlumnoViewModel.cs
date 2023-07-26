using Domain.Alumno;
using Infrastructure;
using System;
using System.Windows.Input;

namespace WPF_Desktop.ViewModels.GestionAlumnos
{
    public class RegistrarAlumnoViewModel : ViewModel
    {
        private UnitOfWork _unitOfWork = new();
        private string _apellido = "Apellido...";
        private string _nombre;
        private string _documento;
        private string _sexo;
        private DateTime _fechaNacimento;
        private string _nacionalidad;

        public ICommand RegistrarAlumno { get; }

        public RegistrarAlumnoViewModel()
        {
            RegistrarAlumno = new ViewModelComand(ExecuteRegistrarAlumnoCommand, CanExecuteRegistrarAlumno);
        }

        public string Apellido
        {
            get
            {
                return _apellido;
            }

            set
            { 
                _apellido = value;
                OnPropertyChanged(nameof(Apellido));
            }
        }

        public string Nombre
        {
            get
            {
                return _nombre;
            }

            set
            {
                _nombre = value;
                OnPropertyChanged(nameof(Nombre));
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

        public string Sexo
        {
            get
            {
                return _sexo;
            }

            set
            {
                _sexo = value;
                OnPropertyChanged(nameof(Sexo));
            }
        }

        public DateTime FechaNacimiento
        {
            get
            {
                return _fechaNacimento;
            }

            set
            {
                _fechaNacimento = value;
                OnPropertyChanged(nameof(FechaNacimiento));
            }
        }

        public string Nacionalidad
        {
            get
            {
                return _nacionalidad;
            }

            set
            {
                _nacionalidad = value;
                OnPropertyChanged(nameof(Nacionalidad));
            }
        }


        private bool CanExecuteRegistrarAlumno(object obj)
        {
            return true;
        }

        private void ExecuteRegistrarAlumnoCommand(object obj)
        {

        }
    }
}
