using Domain.Shared;

namespace Domain.Cursos.Divisiones.Cursantes;

public class Cursante : Entity
{
    private List<Calificacion> _calificaciones;

    public Guid DivisionID { get; private set; }
    public Guid AlumnoID { get; private set; } = Guid.Empty;
    public CicloLectivo CicloLectivo { get; private set; }
    public IReadOnlyCollection<Calificacion> Calificaciones => _calificaciones.AsReadOnly();


    private Cursante() { }

    private Cursante(Guid cursanteID)
        : base(cursanteID) { }

    public Cursante(Guid divisionID, Guid alumnoID, CicloLectivo cicloLectivo)
        : this(Guid.NewGuid())
    {
        if (Guid.Empty.Equals(alumnoID) || cicloLectivo is null)
        {
            throw new NullReferenceException($"Datos del alumno incompletos o inexistentes para registrarlo en la division.");
        }

        DivisionID = divisionID;
        AlumnoID = alumnoID;
        CicloLectivo = cicloLectivo;
        _calificaciones = new List<Calificacion>();
    }

    public void AgregarCalificacion(Calificacion nuevaCalificacion)
    {
       _calificaciones.Add(nuevaCalificacion);
    }

    public void QuitarCalificacion(Calificacion aEliminar)
    {
        _calificaciones.Remove(aEliminar);
    }
}
