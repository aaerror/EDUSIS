using Domain.Shared;

namespace Domain.Usuarios;

public class Acceso : ValueObject
{
    public Permiso Permiso { get; private set; }
    public DateTime FechaAlta { get; private set; } = DateTime.Today;
    public DateTime? FechaBaja { get; private set; } = null;


    private Acceso() { }
    protected Acceso(Permiso unPermiso)
    {
        Permiso = unPermiso;
    }

    protected Acceso(Permiso unPermiso, DateTime fechaAlta, DateTime? fechaBaja)
        : this(unPermiso)
    {
        if (DateTime.Today.Date < fechaAlta.Date)
        {
            throw new ArgumentException("La fecha de alta no puede ser posterior al día de hoy.", nameof(fechaAlta));
        }

        if (fechaBaja.HasValue)
        {
            if (fechaBaja.Value.Date < fechaAlta.Date)
            {
                throw new ArgumentException("La fecha de baja no puede ser anterior a la fecha de alta.", nameof(fechaBaja));
            }

            FechaBaja = fechaBaja.Value.Date;
        }

        FechaAlta = fechaAlta.Date;
    }


    public static Acceso Crear(Permiso permiso) => new(permiso);

    public bool PermisoHabilitado() => !FechaBaja.HasValue;

    public Acceso AnularPermiso() => new(Permiso, FechaAlta, DateTime.Today);

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Permiso;
        yield return FechaAlta;
        yield return FechaBaja;
    }
}
