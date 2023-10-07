using System;
namespace Domain.Docentes;

public class Puesto
{
    public Posicicion Posicion { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime FechaBaja { get; private set; }


    private Puesto(Posicicion Posicion, DateTime fechaAlta, DateTime fechaBaja)
    {
        if (fechaAlta >= DateTime.Now || fechaBaja >= DateTime.Now)
        {
            throw new ArgumentException("Error al cargar el puesto del docente.");
        }

        this.Posicion = Posicion;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
    }

    public static Puesto Create(Posicicion posicion, DateTime fechaAlta, DateTime fechaBaja)
    {
        return new(posicion, fechaAlta, fechaBaja);
    }

    public int AntiguedadEnPuesto()
    {
        int antiguedad = 0;
        if (FechaBaja == null)
        {
            antiguedad = (DateTime.Now - FechaAlta).Days;
        }
        else
        {
            antiguedad = (FechaBaja - FechaAlta).Days;
        }

        return antiguedad/365;
    }

    public Puesto DesactivarPuesto()
    {
        return new(Posicion, FechaAlta, DateTime.Now);
    }
}
