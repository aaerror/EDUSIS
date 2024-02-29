using Domain.Cursos.Divisiones.Cursantes;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursos.Divisiones;

public class Division : Entity
{
    // TODO: VERIFICAR CONSTRAINTS
    private const int MAX_ALUMNOS = 35;
    private const int MIN_ALUMNOS = 15;

    private List<Cursante> _listadosDefinitivos = new();

    public Guid CursoID { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public Guid? Preceptor { get; private set; }
    public int TotalAlumnos => _listadosDefinitivos.Count;
    public IReadOnlyCollection<Cursante> ListadosDefinitivos => _listadosDefinitivos.AsReadOnly();


    protected Division()
        : base() {}

    protected Division(Guid divisionID)
        : base(divisionID) { }

    public Division(Guid cursoID, string descripcion)
        : this(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentNullException(nameof(descripcion), "Datos incompletos del nombre de la división.");
        }

        if (!Regex.IsMatch(descripcion.Trim(), @"^([a-zA-Z]){1}$", RegexOptions.None))
        {
            throw new ArgumentException("La división del curso debe ser una letra.", nameof(descripcion));
        }

        CursoID = cursoID;
        Descripcion = descripcion.ToUpper();
    }

    #region Preceptor
    public bool EstaVacanteCargoPreceptor() => Guid.Empty.Equals(Preceptor);

    public void OcuparCargoPreceptor(Guid preceptorId)
    {
        if (!EstaVacanteCargoPreceptor())
        {
            throw new InvalidOperationException($"El cargo de preceptor no se encuentra vacante. Preceptor: { Preceptor }");
        }

        if (Guid.Empty.Equals(preceptorId))
        {
            throw new ArgumentNullException(nameof(preceptorId), "Se deben especificar que preceptor desea colocar a cargo de la división.");
        }

        if (Preceptor.Equals(preceptorId))
        {
            throw new ArgumentException("El preceptor ya se encuentra inscripto en esta división.", nameof(preceptorId));
        }

        Preceptor = preceptorId;
    }

    public void DesocuparCargoPreceptor()
    {
        if (EstaVacanteCargoPreceptor())
        {
            throw new InvalidOperationException("El cargo de preceptor ya se encuentra vacante.");
        }

        Preceptor = Guid.Empty;
    }
    #endregion

    #region Alumnos
    private Cursante BuscarCursante(Guid unCursante)
    {
        var cursante = _listadosDefinitivos.Where(x => x.Id.Equals(unCursante))
                                           .FirstOrDefault();
        if (cursante is null)
        {
            throw new ArgumentNullException("No se encontró el cursante de la división.");
        }

        return cursante;
    }

    private bool ExisteCursanteInscripto(Guid unAlumno, CicloLectivo cicloLectivo)
    {
        if (Guid.Empty.Equals(unAlumno) || cicloLectivo is null)
        {
            throw new NullReferenceException($"Datos incompletos o inexistentes para comprobar si el alumno se encuentra registrado en la división.");
        }

        return _listadosDefinitivos.Exists(x => x.AlumnoID.Equals(unAlumno) && x.CicloLectivo.Equals(cicloLectivo));
    }

    public void AgregarCursante(Guid unAlumno, string periodo)
    {
        var cicloLectivo = CicloLectivo.Crear(periodo);
        if (ExisteCursanteInscripto(unAlumno, cicloLectivo))
        {
            throw new ArgumentException($"El alumno ya se encuentra registrado en el ciclo lectivo { periodo } para esta división.", nameof(unAlumno));
        }

        var nuevoCursante = new Cursante(Id, unAlumno, cicloLectivo);
        _listadosDefinitivos.Add(nuevoCursante);
    }

    public void QuitarCursante(Guid unAlumno, string periodo)
    {
        var cicloLectivo = CicloLectivo.Crear(periodo);
        var cursante = _listadosDefinitivos.Find(x => x.Id.Equals(unAlumno) && x.CicloLectivo.Equals(cicloLectivo));
        if (cursante is null)
        {
            throw new ArgumentException("El alumno no se encuentra registrado en esta división o en el ciclo lectivo seleccionado.", nameof(unAlumno));
        }

        _listadosDefinitivos.Remove(cursante);
    }

    public IReadOnlyCollection<Guid> ListadoAlumnoCicloLectivoEnCurso()
    {
        return _listadosDefinitivos.Where(x => x.CicloLectivo.AñoEnCurso())
                                   .Select(x => x.AlumnoID).ToList().AsReadOnly();
    }

    public IReadOnlyCollection<Guid> ListadoPorPeriodo(string periodo)
    {
        return _listadosDefinitivos.Where(x => x.CicloLectivo.Equals(CicloLectivo.Crear(periodo)))
                                   .Select(x => x.AlumnoID)
                                   .ToList()
                                   .AsReadOnly();
    }
    #endregion

    #region Calificaciones
    public IReadOnlyCollection<Calificacion> NotasDelAlumno(Guid unAlumno, string periodo)
    {
        var cicloLectivo = CicloLectivo.Crear(periodo);
        var cursante = _listadosDefinitivos.Where(x => x.AlumnoID.Equals(unAlumno) && x.CicloLectivo.Equals(cicloLectivo))
                                           .FirstOrDefault();
        if (cursante is null)
        {
            throw new ArgumentNullException("No se encuentra el alumno registrado en la división ni el ciclo lectivo seleccionado.");
        }

        return cursante.Calificaciones;
    }

    public void AgregarCalificacion(Guid unCursante, Guid unaMateria, bool asistencia, DateTime fecha, Instancia instancia, double? nota)
    {
        var cursante = BuscarCursante(unCursante);
        var nuevaCalificacion = Calificacion.Crear(unaMateria, asistencia, fecha, instancia, nota);
        cursante.AgregarCalificacion(nuevaCalificacion);
    }

    public void QuitarCalificacion(Guid unCursante, Calificacion aEliminar)
    {
        var cursante = BuscarCursante(unCursante);
        cursante.QuitarCalificacion(aEliminar);
    }
    #endregion
}
