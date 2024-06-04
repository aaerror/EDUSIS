using Domain.Cursos.Exceptions;
using Domain.Materias.DomainEvents;
using Domain.Materias.Horarios;
using Domain.Materias.SituacionRevistaDocente;
using Domain.Shared;

namespace Domain.Materias;

public class Materia : Entity
{
    private Guid? _profesorID;
    private readonly List<Horario> _horarios = new();
    private readonly List<SituacionRevista> _profesores = new();

    public Guid CursoID { get; private set; }
    public string Descripcion { get; private set; }
    public int HorasCatedra { get; private set; }
    
    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();
    public IReadOnlyCollection<SituacionRevista> Profesores => _profesores.AsReadOnly();


    protected Materia()
        : base() { }

    protected Materia(Guid materiaID)
        : base(materiaID) { }

    public Materia(Guid cursoID, Guid materiaID, string descripcion, int horasCatedra)
        : this(materiaID)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
        }

        if (horasCatedra <= 0)
        {
            throw new ArgumentException("La cantidad de horas cátedras debe ser mayor a cero.", nameof(horasCatedra));
        }

        CursoID = cursoID;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;

        AgregarEvento(new MateriaRegistradaEvent(Id));
    }

    public Materia(Guid cursoID, string descripcion, int horasCatedra)
        : this(cursoID, Guid.NewGuid(), descripcion, horasCatedra) { }

    public Guid? ProfesorID
    {
        get
        {
            return DocenteEnFunciones()?.ProfesorID ?? Guid.Empty;
        }
        
        private set
        {
            _profesorID = value;
        }
    }

    private void EditarDescripcion(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
        }

        Descripcion = descripcion;
    }

    private void EditarCargaHoraria(int horasCatedra)
    {
        // TODO: Comprobar que la carga horaria sea divisor de 5 o 10
        if (horasCatedra <= 0)
        {
            throw new ArgumentException("La cantidad de horas cátedras debe ser mayor a cero.", nameof(horasCatedra));
        }

        HorasCatedra = horasCatedra;
    }

    public void ModificarMateria(string descripcion, int horasCatedra)
    {
        EditarDescripcion(descripcion);
        EditarCargaHoraria(horasCatedra);
    }

    #region Profesores
    public SituacionRevista? DocenteEnFunciones() =>
        _profesores.Where(x => x.EnFunciones && !x.FechaBaja.HasValue)
                   .FirstOrDefault();

    public SituacionRevista BuscarSituacionRevista(Guid unProfesor)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), "Se deben especificar los datos del profesor que desea buscar.");
        }

        return _profesores.Where(x => x.ProfesorID.Equals(unProfesor) && !x.FechaBaja.HasValue)
                          .FirstOrDefault();
    }

    private bool CargoDisponible(Cargo unCargo) =>
        !_profesores.Any(x => x.Cargo.Equals(unCargo) && !x.FechaBaja.HasValue);

    public bool ExisteDocenteEnFunciones() => _profesores.Any(x => x.EnFunciones && !x.FechaBaja.HasValue);

    public void EstablecerDocenteEnFunciones(Guid unProfesor)
    {
        var situacionRevista = BuscarSituacionRevista(unProfesor);
        if (situacionRevista is null)
        {
            throw new ArgumentException($"El profesor no tiene un cargo en la materia de {Descripcion}.", nameof(unProfesor));
        }

        QuitarDocenteDeFunciones();

        var index = _profesores.IndexOf(situacionRevista);
        _profesores[index] = situacionRevista.EstablecerEnFunciones();
        ProfesorID = unProfesor;
    }

    public void RegistrarNuevaSituacionRevista(Guid unProfesor, Cargo enCargo, DateTime fechaAlta, bool enFunciones)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), $"Se debe especificar el profesor que desea asignar a la materia {Descripcion}.");
        }

        if (!CargoDisponible(enCargo))
        {
            throw new ArgumentException($"Este cargo docente no se encuentra disponible en la materia {Descripcion}.", nameof(enCargo));
        }

        var situacionRevista = BuscarSituacionRevista(unProfesor);
        if (situacionRevista is not null && situacionRevista.FechaBaja is null)
        {
            throw new ArgumentException($"El profesor ya se encuentra con un cargo en la materia. Cargo: { situacionRevista.Cargo } desde el { situacionRevista.FechaAlta.ToString("d") }", nameof(unProfesor));
        }

        var nuevaSituacionRevista = SituacionRevista.Crear(unProfesor, enCargo, fechaAlta, enFunciones);
        if (enFunciones)
        {
            QuitarDocenteDeFunciones();

            _profesores.Add(nuevaSituacionRevista);
            ProfesorID = nuevaSituacionRevista.ProfesorID;

            return;
        }

        _profesores.Add(nuevaSituacionRevista);
    }

    public void CambiarCargoDelProfesor(Guid unProfesor, Cargo nuevoCargo, DateTime fechaAltaCargoNuevo, DateTime fechaBajaCargoAnterior, bool enFunciones)
    {
        if (!CargoDisponible(nuevoCargo))
        {
            throw new ArgumentException($"El cargo {nuevoCargo} ya se encuentra ocupado en la materia {Descripcion}.");
        }

        var situacionRevista = BuscarSituacionRevista(unProfesor);
        if (situacionRevista is null)
        {
            throw new ArgumentException($"El profesor no tiene un cargo en la materia de {Descripcion}.", nameof(unProfesor));
        }

        if (enFunciones && ExisteDocenteEnFunciones())
        {
            throw new ArgumentException($"Error al hacer un cambio de revista. Ya existe un profesor en funciones.", nameof(unProfesor));
        }

        QuitarDocenteDelCargo(unProfesor, fechaBajaCargoAnterior);
        SituacionRevista nuevaSituacionRevista = situacionRevista.CambiarSituacionRevista(nuevoCargo, fechaAltaCargoNuevo, enFunciones);
        _profesores.Add(nuevaSituacionRevista);
    }

    public void QuitarDocenteDelCargo(Guid unProfesor, DateTime fechaBaja)
    {
        var situacionRevista = BuscarSituacionRevista(unProfesor);
        if (situacionRevista is null)
        {
            throw new ArgumentException($"El profesor no tiene un cargo en la materia de {Descripcion}.", nameof(unProfesor));
        }

        var index = _profesores.IndexOf(situacionRevista);
        _profesores[index] = situacionRevista.QuitarCargo(fechaBaja);
    }

    public void QuitarDocenteDeFunciones()
    {
        var aEliminar = DocenteEnFunciones();
        if (aEliminar is not null)
        {
            int index = _profesores.IndexOf(aEliminar);
            _profesores[index] = aEliminar.QuitarDeFunciones();
        }
    }
    #endregion

    #region Horarios
    public int HorasCatedraSinAsignar => HorasCatedra - _horarios.Count();

    public bool HorarioOcupado(Horario horarioBuscado) => _horarios.Contains(horarioBuscado);

    public void AgregarHorario(Horario unHorario)
    {
        if (_horarios.Count >= HorasCatedra)
        {
            throw new ArgumentException("Error al agregar un horario. La materia ya tiene sus horas cátedras completas.");
        }

        _horarios.Add(unHorario);
    }

    public void CambiarHorario(Horario viejo, Horario nuevo)
    {
        if (_horarios.Count() == 0)
        {
            throw new MateriaSinHorariosAsignadoException();
        }

        /*
         * var horario = _horarios.Find(x => x.HoraInicio == horarioViejo.HoraInicio &&
         *                            x.HoraFin == horarioViejo.HoraFin &&
         *                            x.DiaSemana == horarioViejo.DiaSemana);
         */

        if (!_horarios.Contains(viejo))
        {
            throw new ArgumentException("El horario que desea modificar no se encuentra asignado a la materia.", nameof(viejo));
        }

        _horarios.Remove(viejo);
        _horarios.Add(nuevo);
    }

    public bool ExistenHorasCatedraSinAsignar() => HorasCatedraSinAsignar >= 0;
    #endregion
}
