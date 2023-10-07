using Domain.Shared;

namespace Domain.Cursos.Divisiones;

public class Division : Entity

{
    // TODO: VERIFICAR CONSTRAINTS
    private const int MAX_ALUMNOS = 30;
    private const int MIN_ALUMNOS = 10;

    private List<Cursante> _listadosDefinitivos;

    public char Descripcion { get; private set; }
    public Guid Preceptor { get; private set; } = Guid.Empty;
    //public IReadOnlyCollection<Cursante> ListadosDefinitivos => _listadosDefinitivos.AsReadOnly();

    protected Division()
        : base() {}

    protected Division(Guid id)
        : base(id) { }

    public Division(char descripcion)
        : this(new Guid())
    {
        if (!char.IsLetter(descripcion))
        {
            throw new ArgumentException("La división del curso debe ser una letra.", nameof(descripcion));
        }

        Descripcion = char.ToUpper(descripcion);
    }

    #region Preceptor
    public bool EstaVacanteCargoDePreceptor() => Guid.Empty.Equals(Preceptor);

    public void EstablecerPreceptorEnElCargo(Guid preceptorId)
    {
        if (!EstaVacanteCargoDePreceptor())
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

    public void RemoverPreceptorDelCargo()
    {
        if (EstaVacanteCargoDePreceptor())
        {
            throw new InvalidOperationException("El cargo de preceptor ya se encuentra vacante.");
        }

        Preceptor = Guid.Empty;
    }
    #endregion

    #region Alumnos
    public bool ExisteAlumnoEnPeriodoActual(Guid alumnoId)
    {
        return _listadosDefinitivos.Exists(x => x.Alumno.Equals(alumnoId) && x.CicloLectivo.Periodo == DateTime.Now.ToString());
    }

    public void AgregarAlumno(Guid alumnoId, string periodo)
    {
        _listadosDefinitivos.Add(Cursante.Crear(alumnoId, periodo));
    }

    public void AgregarAlumno(Guid alumnoId)
    {
        _listadosDefinitivos.Add(Cursante.Crear(alumnoId, DateTime.Now.Year.ToString()));
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
