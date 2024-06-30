using Domain.Shared;

namespace Domain.Materias.CargosDocentes;

public class SituacionRevista : ValueObject
{
    public Guid DocenteID { get; private set; } = Guid.Empty;
    public Cargo Cargo { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; }
    public bool EnFunciones { get; private set; } = false;


    private SituacionRevista(Guid docenteID, Cargo cargo, DateTime fechaAlta, DateTime? fechaBaja, bool enFunciones)
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

        DocenteID = docenteID;
        Cargo = cargo;
        FechaAlta = fechaAlta.Date;
        EnFunciones = enFunciones;
    }

    private SituacionRevista(Guid docenteID, Cargo cargo, DateTime fechaAlta, bool enFunciones)
        : this(docenteID, cargo, fechaAlta, null, enFunciones) { }

    public static SituacionRevista Crear(Guid docenteID, Cargo cargo, DateTime fechaAlta, bool enFunciones)
    {
        return new(docenteID, cargo, fechaAlta, enFunciones);
    }

    public SituacionRevista CambiarSituacionRevista(Cargo nuevoCargo, DateTime fechaAlta)
    {
        return new(DocenteID, nuevoCargo, fechaAlta, EnFunciones);
    }

    public SituacionRevista QuitarCargo(DateTime fechaBaja)
    {
        return new(DocenteID, Cargo, FechaAlta, fechaBaja.Date, false);
    }

    public SituacionRevista EstablecerEnFunciones()
    {
        return new(DocenteID, Cargo, FechaAlta, true);
    }

    public SituacionRevista QuitarDeFunciones()
    {
        return new(DocenteID, Cargo, FechaAlta, false);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return DocenteID;
        yield return Cargo;
        yield return FechaAlta;
        yield return FechaBaja;
        yield return EnFunciones;
    }
}
