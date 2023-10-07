using Domain.Shared;

namespace Domain.Cursos.Divisiones;

public class Cursante : ValueObject
{
    public Guid Alumno { get; private set; }
    public CicloLectivo CicloLectivo { get; private set; }


    private Cursante() { }
    private Cursante(Guid alumnoId, string cicloLectivo)
    {
        // TODO: Corrobar datos correctos
        Alumno = alumnoId;
        CicloLectivo = CicloLectivo.Crear(cicloLectivo);
    }

    public static Cursante Crear(Guid alumnoId, string cicloLectivo)
    {
        return new(alumnoId, cicloLectivo);
    }

    /*public void AgregarAlumno(Guid aAgregar)
    {
        if (ExisteAlumnoEnListado(aAgregar))
        {
            throw new ArgumentException($"El alumno ya se encuentra en el listado del ciclo lectivo { CicloLectivo }.", nameof(aAgregar));
        }

        _alumnos.Add(aAgregar);
    }

    public void QuitarAlumnoDeListado(Guid aEliminar)
    {
        if (!ExisteAlumnoEnListado(aEliminar))
        {
            throw new ArgumentException($"El alumno no se encuentra en el listado del ciclo lectivo { CicloLectivo.Periodo }.", nameof(aEliminar));
        }

        _alumnos.Remove(aEliminar);
    }*/

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Alumno;
        yield return CicloLectivo;
    }
}
