using Domain.Cursos.Divisiones;
using Domain.Cursos.Divisiones.Cursantes;
using Domain.Cursos.Materias;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursos;

public class Curso : Entity
{
    private List<Materia> _materias = new();
    private List<Division> _divisiones = new();

    // Educación Primaria - Educación Secundaria
    public NivelEducativo NivelEducativo { get; }
    public string Descripcion { get; private set; } = string.Empty;
    public int CantidadDivisiones => _divisiones.Count;
    public int CantidadMaterias => _materias.Count;
    public int CantidadAlumnos => _divisiones.Sum(x => x.TotalAlumnos);
    public IReadOnlyCollection<Materia> Materias => _materias.AsReadOnly();
    public IReadOnlyCollection<Division> Divisiones => _divisiones.AsReadOnly();


    protected Curso()
        : base() { }

    protected Curso(Guid cursoId)
        : base(cursoId) { }

    public Curso(Guid cursoId, string descripcion, NivelEducativo nivelEducativo)
        : this(cursoId)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException("Datos incompletos del nombre del curso.", nameof(descripcion));
        }

        if (!Regex.IsMatch(descripcion.Trim(), @"^(\d){1}$", RegexOptions.None))
        {
            throw new ArgumentException("El año/grado debe ser un número.", nameof(descripcion));
        }

        if (int.Parse(descripcion) > 7)
        {
            throw new ArgumentException("El curso máximo en educación es septimo ya sea en educación primaria o secundaria.", nameof(descripcion));
        }

        Descripcion = descripcion;
        NivelEducativo = nivelEducativo;
        _materias = new List<Materia>();
    }

    public Curso(string descripcion, NivelEducativo nivelEducativo)
        : this(Guid.NewGuid(), descripcion, nivelEducativo) { }

    #region Calificacion
    public void AgregarCalificacion(Guid unaDivision, Guid unCursante, Guid unaMateria, bool asistencia, DateTime fecha, Instancia instancia, double? nota)
    {
        var division = BuscarDivision(unaDivision);
        if (division is null)
        {
            throw new ArgumentException("No se encontró la división en este curso.", nameof(unaDivision));
        }

        if (!ExisteMateria(unaMateria))
        {
            throw new ArgumentException("No se encontró la materia en este curso.", nameof(unaDivision));
        }

        division.AgregarCalificacion(unCursante, unaMateria, asistencia, fecha, instancia, nota);
    }
    #endregion

    #region Materias
    private bool ExisteMateria(Guid unaMateria)
    {
        if (Guid.Empty.Equals(unaMateria))
        {
            throw new ArgumentNullException(nameof(unaMateria), "Debe proporcionar datos de la materia para realizar la búsqueda.");
        }

        return _materias.Exists(x => x.Id.Equals(unaMateria));
    }

    private Materia BuscarMateria(Guid unaMateria)
    {
        if (Guid.Empty.Equals(unaMateria))
        {
            throw new ArgumentNullException(nameof(unaMateria), "Debe proporcionar datos de la materia para realizar la búsqueda.");
        }

        var materia = _materias.Find(x => x.Id.Equals(unaMateria));
        if (materia is null)
        {
            throw new ArgumentException("La materia que esta buscando no se encuentra en este curso.", nameof(unaMateria));
        }

        return materia;
    }

    private bool MateriasConHorasCatedrasSinAsignar() => _materias.Any(x => x.ExistenHorasCatedraSinAsignar());

    public void AgregarMateria(string descripcion, int horasCatedra)
    {
        var materia = _materias.Find(x => x.Descripcion == descripcion);
        if (materia is not null)
        {
            throw new ArgumentException($"La materia { materia.Descripcion } ya se encuentra registrada con { materia.HorasCatedra } horas cátedra.", nameof(descripcion));
        }

        _materias.Add(new Materia(Id, descripcion, horasCatedra));
    }

    public void ActualizarMateria(Guid unaMateria, string descripcion, int horasCatedra)
    {
        var materia = BuscarMateria(unaMateria);

        materia.ActualizarNombre(descripcion);
        materia.ActualizarCargaHoraria(horasCatedra);
    }

    public void QuitarMateria(Guid aEliminar)
    {
        if (Guid.Empty.Equals(aEliminar))
        {
            throw new ArgumentNullException(nameof(aEliminar), "Se debe especificar que materia desea eliminar.");
        }

        var materia = _materias.Find(x => x.Id.Equals(aEliminar));
        if (materia is null)
        {
            throw new ArgumentException($"La materia no se encuentra registrada en el curso { Descripcion } año ({ NivelEducativo })", nameof(aEliminar));
        }

        _materias.Remove(materia);
    }

    public void AgregarHorarioAMateria(Guid materiaId, Horario unHorario)
    {
        var horarioDisponible = !_materias.Any(x => x.HorarioOcupado(unHorario));

        if (!horarioDisponible)
        {
            throw new ArgumentException("El horario que desea asignar a la materia se encuentra ocupado.", nameof(unHorario));
        }

        Materia materiaBuscada = BuscarMateria(materiaId);
        materiaBuscada.AgregarHorario(unHorario);
    }

    public void ModificarHorarioDeMateria(Guid materiaId, Horario antiguo, Horario nuevo)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.CambiarHorario(antiguo, nuevo);
    }

    public IReadOnlyCollection<Materia> ListaMateriasConHorasCatedrasSinAsignar()
    {
        if (!MateriasConHorasCatedrasSinAsignar())
        {
            throw new ArgumentException("No existen materias con horas cátedras sin asignar.");
        }

        return _materias.FindAll(x => x.ExistenHorasCatedraSinAsignar())
                        .ToList()
                        .AsReadOnly();
    }

    public IReadOnlyCollection<Materia> MateriasConCargoVacante()
    {
        return _materias.FindAll(x => !x.ExisteDocenteEnFunciones())
                        .AsReadOnly();
    }
    #endregion

    #region Situacion de Revista
    public SituacionRevista BuscarSituacionRevista(Guid unaMateria, Guid unProfesor)
    {
        Materia materia = BuscarMateria(unaMateria);
        return materia.BuscarCargoDelDocente(unProfesor);
    }

    public void AgregarProfesorEnMateria(Guid unaMateria, Guid unProfesor, Cargo unCargo, DateTime fechaAlta, bool enFunciones)
    {
        Materia materia = BuscarMateria(unaMateria);
        materia.RegistrarNuevaSituacionRevista(unProfesor, unCargo, fechaAlta, enFunciones);
    }

    public void EliminarProfesorDelCargo(Guid unaMateria, Guid unProfesor, DateTime fechaBaja)
    {
        Materia materia = BuscarMateria(unaMateria);
        materia.QuitarDocenteDelCargo(unProfesor, fechaBaja);
    }

    public void EstablecerEnFuncionesAlProfesor(Guid unaMateria, Guid unProfesor)
    {
        Materia materia = BuscarMateria(unaMateria);
        materia.EstablecerDocenteEnFunciones(unProfesor);
    }
    #endregion

    #region Division
    private bool ExisteDivision(Guid unaDivision)
    {
        if (Guid.Empty.Equals(unaDivision))
        {
            throw new ArgumentNullException(nameof(unaDivision), "Se deben especificar los datos de la división.");
        }

        return _divisiones.Any(x => x.Equals(unaDivision));
    }

    private Division BuscarDivision(Guid unaDivision)
    {
        if (!ExisteDivision(unaDivision))
        {
            throw new ArgumentException("La división a la que desea agregar el preceptor no pertenece a este curso.", nameof(unaDivision));
        }

        return _divisiones.Find(x => x.Id.Equals(unaDivision));
    }

    public void AgregarDivision()
    {
        string siguiente = string.Empty;
        var division = _divisiones.Select(x => x.Descripcion)
                                  .OrderDescending()
                                  .FirstOrDefault();
        if (division is null)
        {
            siguiente = "A";
        }
        else
        {
            siguiente = char.ConvertFromUtf32(char.Parse(division) + 1);
        }

        _divisiones.Add(new Division(Id, siguiente));
    }

    public void QuitarDivision(Guid aEliminar)
    {
        var division = _divisiones.Find(x => x.Id.Equals(aEliminar));
        if (division is null)
        {
            throw new ArgumentException("La división que desea eliminar no pertenece a este curso.", nameof(aEliminar));
        }

        _divisiones.Remove(division);
    }

    public IReadOnlyCollection<Guid> ListadoDefinitivoDeAlumnos(Guid unaDivision)
    {
        if (!ExisteDivision(unaDivision))
        {
            throw new ArgumentException($"No se encuentra la división en el curso {Descripcion}", nameof(unaDivision));
        }

        return _divisiones.Find(x => x.Equals(unaDivision)).ListadoAlumnoCicloLectivoEnCurso();
    }

    public IReadOnlyCollection<Guid> CursantesPorPeriodo(Guid unaDivision, string periodo)
    {
        var division = _divisiones.Find(x => x.Id.Equals(unaDivision));
        if (division is null)
        {
            throw new ArgumentException($"No se encuentra la división en el curso { Descripcion }° Año.", nameof(unaDivision));
        }

        return division.ListadoPorPeriodo(periodo);
    }
    #endregion

    #region Preceptor
    public void AsignarPreceptor(Guid unaDivision, Guid unPreceptor)
    {
        Division division = BuscarDivision(unaDivision);
        division.OcuparCargoPreceptor(unPreceptor);
    }

    public void QuitarPreceptor(Guid unaDivision)
    {
        Division division = BuscarDivision(unaDivision);
        division.DesocuparCargoPreceptor();
    }
    #endregion

    #region Alumnos
    public bool ExisteAlumno(Guid unaDivision)
    {
        if (Guid.Empty.Equals(unaDivision))
        {
            throw new NullReferenceException($"Datos de la división incompletos o inexistentes. División: { unaDivision }");
        }

        return _divisiones.Exists(x => x.Id.Equals(unaDivision));
    }

    public void AgregarAlumnoAlListadoDefinitivo(Guid unaDivision, Guid unAlumno, string unPeriodo)
    {
        var division = _divisiones.Find(x => x.Id.Equals(unaDivision));
        if (division is null)
        {
            throw new ArgumentException("La división del curso a la que desea agregar este alumno no existe.", nameof(unaDivision));
        }

        division.AgregarCursante(unAlumno, unPeriodo);
    }

    public void QuitarAlumnoDelListadoDefinitivoActual(Guid unaDivision, Guid unAlumno, string unPeriodo)
    {
        if (ExisteDivision(unaDivision))
        {
            throw new ArgumentException("La división del curso a la que desea agregar este alumno no existe.", nameof(unaDivision));
        }

        var division = _divisiones.Find(x => x.Id.Equals(unaDivision));
        division.QuitarCursante(unAlumno, unPeriodo);
    }
    #endregion
}
