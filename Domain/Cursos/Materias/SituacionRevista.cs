using Domain.Shared;

namespace Domain.Cursos.Materias;

public class SituacionRevista : ValueObject
{
    public Guid ProfesorId { get; private set; } = Guid.Empty;
    public Cargo Cargo { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; }
    public bool EnFunciones { get; private set; } = false;


    private SituacionRevista(Guid profesorId, Cargo cargo, DateTime fechaAlta, DateTime? fechaBaja, bool enFunciones)
    {
        if (fechaAlta.Date > DateTime.Today.Date)
        {
            throw new ArgumentException($"La fecha de alta debe ser anterior al día de hoy ({ DateTime.Today.Date })", nameof(fechaAlta));
        }

        if (fechaBaja is not null)
        {
            if (fechaBaja.Value.Date < fechaAlta.Date)
            {
                throw new ArgumentException("La fecha de baja del cargo no puede ser anterior a la fecha de alta", nameof(fechaBaja));
            }

            FechaBaja = fechaBaja.Value.Date;
        }

        ProfesorId = profesorId;
        Cargo = cargo;
        FechaAlta = fechaAlta.Date;
        EnFunciones = enFunciones;
    }

    private SituacionRevista(Guid profesorId, Cargo cargo, DateTime fechaAlta, bool enFunciones)
        : this(profesorId, cargo, fechaAlta, null, enFunciones) { }

    public static SituacionRevista Crear(Guid profesorId, Cargo cargo, DateTime fechaAlta, bool enFunciones)
    {
        return new(profesorId, cargo, fechaAlta, enFunciones);
    }

    public SituacionRevista CambiarSituacionRevista(Cargo nuevoCargo, DateTime fechaAlta, bool enFunciones)
    {
        return new(ProfesorId, nuevoCargo, fechaAlta, enFunciones);
    }

    public SituacionRevista QuitarCargo(DateTime fechaBaja)
    {
        return new(ProfesorId, Cargo, FechaAlta, fechaBaja, false);
    }

    public SituacionRevista EstablecerEnFunciones()
    {
        return new(ProfesorId, Cargo, FechaAlta, true);
    }

    public SituacionRevista QuitarDeFunciones()
    {
        return new(ProfesorId, Cargo, FechaAlta, false);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return ProfesorId;
        yield return Cargo;
        yield return FechaAlta;
        yield return FechaBaja;
        yield return EnFunciones;
    }
}
