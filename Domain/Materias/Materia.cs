using Domain.Materias.DomainEvents;
using Domain.Materias.Exceptions;
using Domain.Materias.Horarios;
using Domain.Materias.CargosDocentes;
using Domain.Shared;
using MediatR;

namespace Domain.Materias;

public class Materia : Entity
{
    private readonly List<SituacionRevista> _docentes = new();
    private readonly List<Horario> _horarios = new();

    public Guid CursoID { get; private set; }
    public Guid? DocenteID { get => DocenteEnFunciones()?.DocenteID; }
    public SituacionRevista? Docente => DocenteEnFunciones();
    public string Descripcion { get; private set; }
    public int HorasCatedra { get; private set; }
    public IReadOnlyCollection<SituacionRevista> Docentes => _docentes.ToList();
    public IReadOnlyCollection<Horario> Horarios => _horarios.ToList();


    private Materia()
        : base() { }

    private Materia(Guid materiaID)
        : base(materiaID) { }

    private Materia(Guid materiaID, Guid cursoID, string descripcion, int horasCatedra)
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
        : this(Guid.NewGuid(), cursoID, descripcion, horasCatedra) { }

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

    #region Docente
    private SituacionRevista? DocenteEnFunciones() =>
        _docentes.Where(x => x.EnFunciones && !x.FechaBaja.HasValue)
                 .FirstOrDefault();

    private SituacionRevista? BuscarSituacionRevistaActiva(Guid unDocente)
    {
        if (Guid.Empty.Equals(unDocente))
        {
            throw new ArgumentNullException(nameof(unDocente), "Se deben especificar los datos del docente que desea buscar.");
        }

        return _docentes.Where(x => x.DocenteID.Equals(unDocente) && !x.FechaBaja.HasValue)
                        .FirstOrDefault();
    }

    private bool CargoDisponible(Cargo unCargo) =>
        !_docentes.Any(x => x.Cargo.Equals(unCargo) && !x.FechaBaja.HasValue);

    private void DarBajaCargoDocente(SituacionRevista aModificar)
    {
        var index = _docentes.IndexOf(aModificar);
        _docentes[index] = aModificar.QuitarCargo(DateTime.Now.Date);
    }

    public int CargosOcupados() =>
        Docentes
            .Where(x => x.EnFunciones)
            .Count();

    public void DarBajaCargoDocente(Guid unDocente)
    {
        var aModificar = BuscarSituacionRevistaActiva(unDocente);
        DarBajaCargoDocente(aModificar);
    }

    public void RegistrarCargoDocente(Guid docenteID, Cargo cargo, DateTime fechaAlta, bool enFunciones)
    {
        if (!CargoDisponible(cargo))
        {
            throw new ArgumentException($"Ya existe un docente ocupando este cargo ({ cargo }) en la materia. Debe dar de baja ese cargo antes de continuar.", nameof(cargo));
        }

        var situacionRevista = BuscarSituacionRevistaActiva(docenteID);
        var unaSituacionRevista = SituacionRevista.Crear(docenteID, cargo, fechaAlta, enFunciones);

        if (situacionRevista is null)
        {
            _docentes.Add(unaSituacionRevista);
        }
        else
        {
            DarBajaCargoDocente(situacionRevista);
            _docentes.Add(unaSituacionRevista);
        }
    }

    public void ModificarCargoDocente(Guid docenteID, Cargo cargo, DateTime fechaAlta)
    {
        //GP87WFW2
        if (!CargoDisponible(cargo))
        {
            throw new ArgumentException($"Ya existe un docente ocupando este nuevo cargo ({cargo}) en la materia. Debe dar de baja ese cargo antes de continuar.", nameof(cargo));
        }

        var aModificar = BuscarSituacionRevistaActiva(docenteID);
        var index = _docentes.IndexOf(aModificar);
        _docentes[index] = aModificar.CambiarSituacionRevista(cargo, fechaAlta);
    }

    public void AsignarDocenteDeAula(Guid unDocente)
    {
        var situacionRevista = BuscarSituacionRevistaActiva(unDocente);
        if (situacionRevista is null)
        {
            throw new ArgumentNullException(nameof(unDocente), "El docente no tiene un cargo asignado en la materia.");
        }

        if (situacionRevista.EnFunciones)
        {
            throw new ArgumentException(nameof(unDocente), "El docente ya se encuentra como docente de aula en la materia.");
        }

        _docentes.Remove(situacionRevista);
        _docentes.Add(situacionRevista.EstablecerEnFunciones());
    }
    #endregion

    #region Horarios
    public int HorasCatedraSinAsignar =>
        HorasCatedra - _horarios.Count();

    public bool HorarioOcupado(Horario horarioBuscado) => _horarios.Contains(horarioBuscado);

    public void AgregarHorario(Turno unTurno, Dia unDia, TimeOnly horaInicio, int duracion)
    {
        if (_horarios.Count >= HorasCatedra)
        {
            throw new ArgumentException("Error al agregar un horario. La materia ya tiene sus horas cátedras completas.");
        }

        var unHorario = Horario.Crear(unTurno, unDia, horaInicio, duracion);
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

    public bool ExistenHorasCatedraSinAsignar() =>
        HorasCatedraSinAsignar >= 0;
    #endregion
}
