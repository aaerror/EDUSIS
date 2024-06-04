using Domain.Cursos.Divisiones;
using Domain.Cursos.Divisiones.Cursantes;
using Domain.Cursos.DomainEvents;
using Domain.Materias;
using Domain.Materias.Horarios;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursos;

public class Curso : Entity
{
    private List<Guid> _materias = new();
    private List<Division> _divisiones = new();

    // Educación Primaria - Educación Secundaria
    public NivelEducativo NivelEducativo { get; }
    public string Descripcion { get; private set; } = string.Empty;
    public int CantidadDivisiones => _divisiones.Count;
    public int CantidadMaterias => _materias.Count;
    public int CantidadAlumnos => _divisiones.Sum(x => x.TotalAlumnos);
    public IReadOnlyCollection<Guid> Materias => _materias.AsReadOnly();
    public IReadOnlyCollection<Division> Divisiones => _divisiones.AsReadOnly();


    protected Curso()
        : base() { }

    protected Curso(Guid cursoId)
        : base(cursoId) { }

    protected Curso(Guid cursoId, string descripcion, NivelEducativo nivelEducativo)
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
        _materias = new List<Guid>();
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

        return _materias.Any(x => x.Equals(unaMateria));
    }

    public void AgregarMateria(Guid nuevaMateria)
    {
        if (ExisteMateria(nuevaMateria))
        {
            throw new ArgumentException($"La materia ya se encuentra registrada en el curso { Descripcion }° año ({ NivelEducativo })", nameof(nuevaMateria));
        }

        _materias.Add(nuevaMateria);
    }

    public void QuitarMateria(Guid aEliminar)
    {
        if (Guid.Empty.Equals(aEliminar))
        {
            throw new ArgumentNullException(nameof(aEliminar), "Se debe especificar que materia desea eliminar.");
        }

        if (!ExisteMateria(aEliminar))
        {
            throw new ArgumentException($"La materia no se encuentra registrada en el curso { Descripcion } año ({ NivelEducativo })", nameof(aEliminar));
        }

        _materias.Remove(aEliminar);
        AgregarEvento(new MateriaEliminadaEvent(aEliminar));
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
