using Domain.Cursos.Exceptions;
using Domain.Shared;

namespace Domain.Cursos.Materias;

public class Materia : Entity
{
    private readonly List<Horario> _horarios = new();
    private readonly List<SituacionRevista> _profesores = new();

    public string Descripcion { get; private set; }
    public int HorasCatedra { get; private set; }
    public Guid Profesor { get; private set; } = Guid.Empty;
    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();
    public IReadOnlyCollection<SituacionRevista> Profesores => _profesores.AsReadOnly();


    protected Materia()
        : base() { }

    protected Materia(Guid materiaId)
        : base(materiaId) { }

    public Materia(Guid materiaId, string descripcion, int horasCatedra)
        : this(materiaId)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
        }

        if (horasCatedra <= 0)
        {
            throw new ArgumentException("La cantidad de horas cátedras debe ser mayor a cero.", nameof(horasCatedra));
        }

        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
    }

    public Materia(string descripcion, int horasCatedra)
        : this(Guid.NewGuid(), descripcion, horasCatedra)
    { }

    #region Docente
    public bool ExisteProfesorEnFunciones() => !Guid.Empty.Equals(Profesor);

    public void EstablecerEnFunciones(Guid unProfesor)
    {
        if (!ExisteProfesorEnMateria(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia {Descripcion}.", nameof(unProfesor));
        }

        Profesor = unProfesor;
    }

    public bool CargoDisponible(Cargo unCargo) => !_profesores.Any(x => x.Cargo.Equals(unCargo));

    public bool ExisteProfesorEnMateria(Guid unProfesor)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), "Se deben especificar los datos del profesor que desea buscar.");
        }

        return _profesores.Any(x => x.ProfesorId.Equals(unProfesor));
    }

    public SituacionRevista BuscarSituacionRevistaDelProfesor(Guid unProfesor)
    {
        if (ExisteProfesorEnMateria(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia {Descripcion}.", nameof(unProfesor));
        }

        return _profesores.Where(x => x.ProfesorId.Equals(unProfesor))
                          .FirstOrDefault();
    }

    public void AgregarProfesorEnCargo(Guid unProfesor, Cargo enCargo)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), $"Se debe especificar el profesor que desea asignar a la materia {Descripcion}.");
        }

        if (CargoDisponible(enCargo))
        {
            throw new ArgumentException($"Este cargo docente no se encuentra disponible en la materia {Descripcion}.", nameof(enCargo));
        }

        _profesores.Add(SituacionRevista.Crear(unProfesor, enCargo));
    }

    public void CambiarSituacionRevista(Guid unProfesor, Cargo nuevoCargo)
    {
        if (!ExisteProfesorEnMateria(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia {Descripcion}.", nameof(unProfesor));
        }

        if (!CargoDisponible(nuevoCargo))
        {
            throw new ArgumentException($"El cargo {nuevoCargo} se encuentra ocupado en la materia {Descripcion}.");
        }

        var situacionRevista = BuscarSituacionRevistaDelProfesor(unProfesor);
        SituacionRevista nuevaSituacionRevista = situacionRevista.CambiarSituacionRevista(nuevoCargo);

        _profesores.Remove(situacionRevista);
        _profesores.Add(nuevaSituacionRevista);
    }

    public void EliminarProfesorDelCargo(Guid unProfesor)
    {
        if (!ExisteProfesorEnMateria(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia {Descripcion}.", nameof(unProfesor));
        }

        _profesores.RemoveAll(x => x.ProfesorId.Equals(unProfesor));
        if (Profesor.Equals(unProfesor))
        {
            Profesor = Guid.Empty;
        }
    }
    #endregion

    #region Horarios
    private int HorasCatedrasSinAsignar => HorasCatedra - _horarios.Count();

    public bool HorarioOcupado(Horario horarioBuscado) => _horarios.Contains(horarioBuscado);

    public void AsignarHorario(Horario unHorario)
    {
        if (_horarios.Count >= HorasCatedra)
        {
            throw new ArgumentException("Error al agregar un horario. La materia ya tiene sus horas cátedras completas.");
        }

        _horarios.Add(unHorario);
    }

    public void CambiarHorario(Horario viejo, Horario nuevo)
    {
        if(_horarios.Count() == 0)
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

    public bool ExistenHorasCatedrasSinAsignar() => HorasCatedrasSinAsignar >= 0;
    #endregion
}
