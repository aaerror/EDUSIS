using Domain.Shared;

namespace Domain.Cursos.Divisiones;

public class Cursante : ValueObject
{
    public Guid Alumno { get; private set; } = Guid.Empty;
    public CicloLectivo CicloLectivo { get; private set; }


    private Cursante() { }

    private Cursante(Guid alumnoId, CicloLectivo cicloLectivo)
    {
        if (Guid.Empty.Equals(alumnoId))
        {
            throw new NullReferenceException($"Datos del alumno incompletos o inexistentes. Alumno: {alumnoId}");
        }

        Alumno = alumnoId;
        CicloLectivo = cicloLectivo;
    }

    private Cursante(Guid alumnoId, string cicloLectivo) : this(alumnoId, CicloLectivo.Crear(cicloLectivo)) { }

    public static Cursante Crear(Guid alumnoId, string cicloLectivo)
    {
        return new(alumnoId, cicloLectivo);
    }

    public static Cursante Crear(Guid alumnoId, CicloLectivo cicloLectivo)
    {
        return new(alumnoId, cicloLectivo);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Alumno;
        yield return CicloLectivo;
    }
}
