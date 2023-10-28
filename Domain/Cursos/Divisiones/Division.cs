using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursos.Divisiones;

public class Division : Entity
{
    // TODO: VERIFICAR CONSTRAINTS
    private const int MAX_ALUMNOS = 30;
    private const int MIN_ALUMNOS = 10;

    private List<Cursante> _listadosDefinitivos = new();

    public string Descripcion { get; private set; } = string.Empty;
    public Guid? Preceptor { get; private set; }
    public int TotalAlumnos => _listadosDefinitivos.Count;

    //public IReadOnlyCollection<Cursante> ListadosDefinitivos => _listadosDefinitivos.AsReadOnly();


    protected Division()
        : base() {}

    protected Division(Guid id)
        : base(id) { }

    public Division(string descripcion)
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
    private bool ExisteCursanteInscripto(Guid alumnoId, CicloLectivo periodo)
    {
        if (Guid.Empty.Equals(alumnoId))
        {
            throw new NullReferenceException($"Datos del alumno incompletos o inexistentes. Alumno: { alumnoId }");
        }

        return _listadosDefinitivos.Exists(x => x.Alumno.Equals(alumnoId) && x.CicloLectivo.Periodo.Equals(periodo));
    }

    public void AgregarCursante(Guid alumnoId, string periodo)
    {
        var nuevoCursante = Cursante.Crear(alumnoId, periodo);
        if (ExisteCursanteInscripto(nuevoCursante.Alumno, nuevoCursante.CicloLectivo))
        {
            throw new ArgumentException($"El alumno ya se encuentra registrado en el ciclo lectivo { periodo } para esta división.", nameof(alumnoId));
        }

        _listadosDefinitivos.Add(nuevoCursante);
    }

    public void QuitarCursanteEnCurso(Guid alumnoId)
    {
        var cicloLectivo = CicloLectivo.Crear(DateTime.Now.Year.ToString());
        if (!ExisteCursanteInscripto(alumnoId, cicloLectivo))
        {
            throw new ArgumentException("El alumno no se encuentra registrado en esta división en el ciclo lectivo en curso.", nameof(alumnoId));
        }

        _listadosDefinitivos.Remove(Cursante.Crear(alumnoId, cicloLectivo));
    }

    public IReadOnlyCollection<Guid> ListadoAlumnoCicloLectivoEnCurso()
    {
        return _listadosDefinitivos.Where(x => x.CicloLectivo.AñoEnCurso())
                                   .Select(x => x.Alumno).ToList().AsReadOnly();
    }

    public IReadOnlyCollection<Guid> ListadoAlumnoSegunCicloLectivo(string periodo)
    {
        return _listadosDefinitivos.Where(x => x.CicloLectivo.Equals(CicloLectivo.Crear(periodo)))
                                   .Select(x => x.Alumno).ToList().AsReadOnly();
    }
    #endregion
}
