using Domain.Cursos.Divisiones;
using Domain.Cursos.Materias;
using Domain.Docentes;
using Domain.Shared;

namespace Domain.Cursos;

public class Curso : Entity
{
    private List<Materia> _materias;
    private List<Division> _divisiones;

    // Educación Primaria - Educación Secundaria
    public Formacion Formacion { get; }
    public char Descripcion { get; private set; }
    public IReadOnlyCollection<Materia> Materias => _materias.AsReadOnly();
    public IReadOnlyCollection<Division> Divisiones => _divisiones.AsReadOnly();


    protected Curso()
        : base() {}

    protected Curso(Guid cursoId)
        : base(cursoId) {}

    public Curso(Guid cursoId, char descripcion, Formacion formacion)
        : this(cursoId)
    {
        if (!char.IsNumber(descripcion))
        {
            throw new ArgumentException("El año/grado debe ser un número.", nameof(descripcion));
        }

        if (int.Parse(descripcion.ToString()) > 7)
        {
            throw new ArgumentException("El curso máximo en educación es septimo ya sea en educación primaria o secundaria.", nameof(descripcion));
        }

        Formacion = formacion;
        _materias = new List<Materia>();
    }

    public Curso(char descripcion, Formacion formacion)
        : this(new Guid(), descripcion, formacion) { }

    #region Division
    private bool ExisteDivision(Guid unaDivision)
    {
        if (Guid.Empty.Equals(unaDivision))
        {
            throw new ArgumentNullException(nameof(unaDivision), "Se deben especificar los datos de la división.");
        }

        return _divisiones.Any(x => x.Equals(unaDivision));
    }

    public IReadOnlyCollection<Guid> ListadoDefinitivoDeAlumnos(Guid unaDivision)
    {
        if (!ExisteDivision(unaDivision))
        {
            throw new ArgumentException($"No se encuentra la división en el curso { Descripcion }", nameof(unaDivision));
        }

        return _divisiones.Find(x => x.Equals(unaDivision)).ListadoAlumnoCicloLectivoEnCurso();
    }

    public IReadOnlyCollection<Guid> ListadoDefinitivoDeAlumnosSegunCicloLectivo(Guid unaDivision, string periodo)
    {
        if (!ExisteDivision(unaDivision))
        {
            throw new ArgumentException($"No se encuentra la división en el curso {Descripcion}", nameof(unaDivision));
        }

        return _divisiones.Find(x => x.Equals(unaDivision)).ListadoAlumnoSegunCicloLectivo(periodo);
    }

    public void AgregarDivisionAlCurso(char descripcion)
    {
        _divisiones.Add(new Division(descripcion));
    }

    public void QuitarDivisionDelCurso(Guid aEliminar)
    {
        var division = _divisiones.Find(x => x.Id.Equals(aEliminar));

        if (division is null)
        {
            throw new ArgumentException("La división que desea eliminar no pertenece a este curso.", nameof(aEliminar));
        }

        _divisiones.Remove(division);
    }
    #endregion

    #region Materias
    private Materia BuscarMateria(Guid materiaId)
    {
        if (Guid.Empty.Equals(materiaId))
        {
            throw new ArgumentNullException(nameof(materiaId), "Debe proporcionar datos de la materia para realizar la búsqueda.");
        }

        var materia = _materias.Find(x => x.Id.Equals(materiaId));
        if (materia is null)
        {
            throw new ArgumentException("La materia que esta buscando no se encuentra en este curso.", nameof(materiaId));
        }

        return materia;
    }

    public void AgregarMateria(string descripcion, int horasCatedra)
    {
        _materias.Add(new Materia(descripcion, horasCatedra));
    }

    public void AsignarHorarioAMateria(Guid materiaId, Horario unHorario)
    {
        var horarioDisponible = !_materias.Any(x => x.HorarioOcupado(unHorario));

        if (!horarioDisponible)
        {
            throw new ArgumentException("El horario que desea asignar a la materia se encuentra ocupado.", nameof(unHorario));
        }

        Materia materiaBuscada = BuscarMateria(materiaId);
        materiaBuscada.AsignarHorario(unHorario);
    }

    public void ModificarHorarioDeMateria(Guid materiaId, Horario antiguo, Horario nuevo)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.CambiarHorario(antiguo, nuevo);
    }

    public bool ExistenMateriasConHorasCatedrasSinAsignar() => _materias.Any(x => x.ExistenHorasCatedrasSinAsignar());

    public IReadOnlyCollection<Guid> MateriasConHorasCatedrasSinAsignar()
    {
        if (!ExistenMateriasConHorasCatedrasSinAsignar())
        {
            throw new ArgumentException("No existen materias con horas cátedras sin asignar.");
        }

        return _materias.FindAll(x => x.ExistenHorasCatedrasSinAsignar())
                        .Select(x => x.Id)
                        .ToList()
                        .AsReadOnly();
    }

    public IReadOnlyCollection<Guid> MateriasConCargoVacante()
    {
        return _materias.FindAll(x => !x.ExisteProfesorEnFunciones())
                        .Select(x => x.Id)
                        .ToList()
                        .AsReadOnly();
    }

    public void AsignarProfesorAMateria(Guid materiaId, Guid profesor, Cargo unCargo)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.AgregarProfesorEnCargo(profesor, unCargo);
    }

    public void CambiarSituacionRevista(Guid materiaId, Guid profesor, Cargo unCargo)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.CambiarSituacionRevista(profesor, unCargo);
    }

    public void EliminarProfesorDelCargo(Guid materiaId, Guid profesor)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.EliminarProfesorDelCargo(profesor);
    }

    public void EstablecerEnFuncionesAlProfesor(Guid materiaId, Guid profesor)
    {
        Materia materia = BuscarMateria(materiaId);
        materia.EstablecerEnFunciones(profesor);
    }
    #endregion

    /*
    #region Alumnos
    public void AgregarAlumnoADivision(Guid divisionId, Guid alumnoId)
    {
        var alumnoInscripto = _divisiones.Exists(x => x.ExisteAlumno(alumnoId));
        if (alumnoInscripto)
        {
            throw new ArgumentException("El alumno ya se encuentra inscripto en otra división.", nameof(alumnoId));
        }

        var division = _divisiones.Find(x => x.Id == curosId);
        division.AgregarAlumno(alumnoId);
    }
    
    public void CambiarAlumnoDeDivision(Guid division, Guid alumnoId)
    {
        var divisionAntigua = _divisiones.Find(x => x.ExisteAlumno(alumnoId));
        if (divisionAntigua is null)
        {
            throw new ArgumentNullException(nameof(alumnoId), $"No se encontró el alumno en el curso { Descripcion }.");
        }

        divisionAntigua.QuitarAlumno(alumnoId);

        var nuevaDivision = _divisiones.Find(x => x.Id == division);
        nuevaDivision.AgregarAlumno(alumnoId);
    }
    */

    /* public void QuitarAlumnoDeDivision(Guid curosId, Guid alumnoId)
     {
         var division = _divisiones.Find(x => x.Id == curosId);
         division.QuitarAlumno(alumnoId);
     }

         #endregion*/
}
