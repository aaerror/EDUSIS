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

    public void ActualizarNombre(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
        }

        Descripcion = descripcion;
    }

    public void ActualizarCargaHoraria(int horasCatedra)
    {
        if (horasCatedra <= 0)
        {
            throw new ArgumentException("La cantidad de horas cátedras debe ser mayor a cero.", nameof(horasCatedra));
        }

        HorasCatedra = horasCatedra;
    }

    #region Docente
    private SituacionRevista BuscarCargoDelProfesor(Guid unProfesor)
    {
        if (ExisteProfesorConCargoAsignado(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia {Descripcion}.", nameof(unProfesor));
        }

        return _profesores.Where(x => x.ProfesorId.Equals(unProfesor))
                          .FirstOrDefault();
    }

    private bool CargoDisponible(Cargo unCargo) => !_profesores.Any(x => x.Cargo.Equals(unCargo));

    private bool ExisteProfesorConCargoAsignado(Guid unProfesor)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), "Se deben especificar los datos del profesor que desea buscar.");
        }

        return _profesores.Any(x => x.ProfesorId.Equals(unProfesor));
    }


    public bool ExisteProfesorEnFunciones() => !Guid.Empty.Equals(Profesor);

    public void EstablecerProfesorEnFunciones(Guid unProfesor)
    {
        if (!ExisteProfesorConCargoAsignado(unProfesor))
        {
            throw new ArgumentException($"El profesor no tiene un cargo en la materia de { Descripcion }.", nameof(unProfesor));
        }

        Profesor = unProfesor;
    }

    public void AsignarCargoDelProfesor(Guid unProfesor, Cargo enCargo, DateTime fechaAlta)
    {
        if (Guid.Empty.Equals(unProfesor))
        {
            throw new ArgumentNullException(nameof(unProfesor), $"Se debe especificar el profesor que desea asignar a la materia {Descripcion}.");
        }

        if (CargoDisponible(enCargo))
        {
            throw new ArgumentException($"Este cargo docente no se encuentra disponible en la materia { Descripcion }.", nameof(enCargo));
        }

        var situacionRevista = BuscarCargoDelProfesor(unProfesor);
        if (situacionRevista is not null || situacionRevista.FechaBaja is null)
        {
            throw new ArgumentException($"El profesor ya se encuentra con un cargo en la materia. Cargo: { situacionRevista.Cargo } desde el { situacionRevista.FechaAlta.ToString("d") }", nameof(unProfesor));
        }

        _profesores.Add(SituacionRevista.Crear(unProfesor, enCargo, fechaAlta));
    }

    public void CambiarCargoDelProfesor(Guid unProfesor, Cargo nuevoCargo, DateTime fechaAltaCargoNuevo, DateTime fechaBajaCargoAnterior)
    {
        if (!ExisteProfesorConCargoAsignado(unProfesor))
        {
            throw new ArgumentException($"No existe el profesor con un cargo en la materia { Descripcion }.", nameof(unProfesor));
        }

        if (!CargoDisponible(nuevoCargo))
        {
            throw new ArgumentException($"El cargo { nuevoCargo } ya se encuentra ocupado en la materia { Descripcion }.");
        }

        QuitarProfesorDelCargo(unProfesor, fechaBajaCargoAnterior);
        var situacionRevista = BuscarCargoDelProfesor(unProfesor);

        SituacionRevista nuevaSituacionRevista = situacionRevista.CambiarSituacionRevista(nuevoCargo, fechaAltaCargoNuevo);
        _profesores.Add(nuevaSituacionRevista);
    }

    public void QuitarProfesorDelCargo(Guid unProfesor, DateTime fechaBaja)
    {
        if (!ExisteProfesorConCargoAsignado(unProfesor))
        {
            throw new ArgumentException($"El profesor no se encuentra inscripto en la materia de {Descripcion}.", nameof(unProfesor));
        }

        var situacionRevista = BuscarCargoDelProfesor(unProfesor);

        var index = _profesores.IndexOf(situacionRevista);
        _profesores[index] = situacionRevista.QuitarCargo(fechaBaja);
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

    public bool ExistenHorasCatedraSinAsignar() => HorasCatedraSinAsignar >= 0;
    #endregion
}
