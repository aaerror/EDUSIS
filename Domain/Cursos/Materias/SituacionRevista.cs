using Domain.Shared;

namespace Domain.Cursos.Materias;

public class SituacionRevista : ValueObject
{
    public Guid ProfesorId { get; private set; } = Guid.Empty;
    public Cargo Cargo { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; }


    private SituacionRevista(Guid profesorId, Cargo cargo, DateTime fechaAlta, DateTime? fechaBaja)
    {
        if (fechaAlta.Date > DateTime.Now.Date)
        {
            throw new ArgumentException($"La fecha de alta debe ser anterior al día de hoy ({ DateTime.Now.Date })", nameof(fechaAlta));
        }

        if (fechaBaja is not null)
        {
            if (fechaBaja < fechaAlta)
            {
                throw new ArgumentException("La fecha de baja del cargo no puede ser anterior a la fecha de alta", nameof(fechaBaja));
            }

            FechaBaja = fechaBaja;
        }

        ProfesorId = profesorId;
        Cargo = cargo;
        FechaAlta = fechaAlta;
    }

    private SituacionRevista(Guid profesorId, Cargo cargo, DateTime fechaAlta)
        : this(profesorId, cargo, fechaAlta, null) { }

    public static SituacionRevista Crear(Guid profesorId, Cargo cargo, DateTime fechaAlta)
    {
        return new(profesorId, cargo, fechaAlta);
    }

    public SituacionRevista CambiarSituacionRevista(Cargo nuevoCargo, DateTime fechaAlta)
    {
        return new(ProfesorId, nuevoCargo, fechaAlta);
    }

    public SituacionRevista QuitarCargo(DateTime fechaBaja)
    {
        return new(ProfesorId, Cargo, FechaAlta, fechaBaja);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return ProfesorId;
        yield return Cargo;
        yield return FechaAlta;
        yield return FechaBaja;
    }
}
