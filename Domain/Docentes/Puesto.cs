using Domain.Shared;

namespace Domain.Docentes;

public class Puesto : ValueObject
{
    public Posicion Posicion { get; private set; }
    public DateTime FechaInicio { get; private set; }
    public DateTime? FechaFin { get; private set; }


    private Puesto(Posicion posicion, DateTime fechaInicio)
    {
        if (fechaInicio >= DateTime.Today.Date)
        {
            throw new ArgumentException("Error al registrar la fecha de alta del puesto del docente.", nameof(fechaInicio));
        }

        Posicion = posicion;
        FechaInicio = fechaInicio.Date;
    }

    private Puesto(Posicion posicion, DateTime fechaInicio, DateTime? fechaFin)
        : this (posicion, fechaInicio)
    {
        if (fechaFin is not null)
        {
            if (fechaFin > DateTime.Today.Date)
            {
                throw new ArgumentException("Error al cargar la fecha de baja del puesto del docente.", nameof(fechaInicio));
            }

            FechaFin = fechaFin.Value.Date;
        }
    }

    public static Puesto Create(Posicion posicion, DateTime fechaInicio)
    {
        return new(posicion, fechaInicio);
    }

    public int AntiguedadEnPuesto()
    {
        int antiguedad = 0;
        if (FechaFin == null)
        {
            antiguedad = (DateTime.Today.Date - FechaInicio.Date).Days;
        }
        else
        {
            antiguedad = FechaFin.Value.Subtract(FechaInicio).Days;
        }

        return antiguedad/365;
    }

    public Puesto QuitarPuesto() => new(Posicion, FechaInicio, DateTime.Today.Date);

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Posicion;
        yield return FechaInicio;
        yield return FechaFin;
    }
}
