using Domain.Shared;

namespace Domain.Cursos.Materias;

public class SituacionRevista : ValueObject
{
    public Guid ProfesorId { get; private set; } = Guid.Empty;
    public Cargo Cargo { get; private set; }


    private SituacionRevista(Guid profesorId, Cargo cargo)
    {
        ProfesorId = profesorId;
        Cargo = cargo;
    }

    public static SituacionRevista Crear(Guid profesorId, Cargo cargo)
    {
        return new(profesorId, cargo);
    }

    public SituacionRevista CambiarSituacionRevista(Cargo nuevoCargo)
    {
        return new(ProfesorId, nuevoCargo);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return ProfesorId;
        yield return Cargo;
    }
}
