using Domain.Shared;

namespace Domain.Materias.SituacionRevistaDocente;

public class SituacionRevista : ValueObject
{
    public Guid ProfesorID { get; private set; } = Guid.Empty;
    public Cargo Cargo { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; }
    public bool EnFunciones { get; private set; } = false;


    private SituacionRevista(Guid profesorID, Cargo cargo, DateTime fechaAlta, DateTime? fechaBaja, bool enFunciones)
    {
        if (fechaAlta.Date > DateTime.Today.Date)
        {
            throw new ArgumentException($"La fecha de alta debe ser anterior al día de hoy ({DateTime.Today.Date})", nameof(fechaAlta));
        }

        if (fechaBaja is not null)
        {
            if (fechaBaja.Value.Date < fechaAlta.Date)
            {
                throw new ArgumentException("La fecha de baja del cargo no puede ser anterior a la fecha de alta", nameof(fechaBaja));
            }

            FechaBaja = fechaBaja.Value.Date;
        }

        ProfesorID = profesorID;
        Cargo = cargo;
        FechaAlta = fechaAlta.Date;
        EnFunciones = enFunciones;
    }

    private SituacionRevista(Guid profesorID, Cargo cargo, DateTime fechaAlta, bool enFunciones)
        : this(profesorID, cargo, fechaAlta, null, enFunciones) { }

    public static SituacionRevista Crear(Guid profesorID, Cargo cargo, DateTime fechaAlta, bool enFunciones)
    {
        return new(profesorID, cargo, fechaAlta, enFunciones);
    }

    public SituacionRevista CambiarSituacionRevista(Cargo nuevoCargo, DateTime fechaAlta, bool enFunciones)
    {
        return new(ProfesorID, nuevoCargo, fechaAlta, enFunciones);
    }

    public SituacionRevista QuitarCargo(DateTime fechaBaja)
    {
        return new(ProfesorID, Cargo, FechaAlta, fechaBaja, false);
    }

    public SituacionRevista EstablecerEnFunciones()
    {
        return new(ProfesorID, Cargo, FechaAlta, true);
    }

    public SituacionRevista QuitarDeFunciones()
    {
        return new(ProfesorID, Cargo, FechaAlta, false);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return ProfesorID;
        yield return Cargo;
        yield return FechaAlta;
        yield return FechaBaja;
        yield return EnFunciones;
    }
}
