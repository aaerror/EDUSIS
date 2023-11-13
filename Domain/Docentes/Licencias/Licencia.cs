using Domain.Personas.Exceptions;
using Domain.Shared;
using System.Diagnostics.Contracts;

namespace Domain.Docentes.Licencias;

public class Licencia : ValueObject
{
    public Articulo Articulo { get; private set; }
    public Estado Estado { get; private set; }
    public int Dias { get; private set; } = 0;
    public DateTime FechaInicio { get; private set; } = DateTime.Today.Date;
    public DateTime FechaFin { get; } = DateTime.Today.Date;
    public string Observacion { get; private set; } = string.Empty;


    private Licencia(Articulo articulo, Estado estado, int dias, DateTime fechaInicio, string observacion)
    {
        if (fechaInicio.Date > DateTime.Today.Date)
        {
            throw new ArgumentException($"La fecha de inicio no debe mayor a la fecha de hoy ({ DateTime.Today.Date.ToString("D") })");
        }

        if (dias <= 0)
        {
            throw new ArgumentException("Se debe especificar la cantidad de días de la licencia.");
        }

        if (!string.IsNullOrWhiteSpace(observacion))
        {
            if (observacion.Trim().Length > 120)
            {
                throw new ExcesoCaracteresException("La observación no debe superar los 120 caracteres.");
            }

            Observacion = observacion.Trim();
        }

        Articulo = articulo;
        Estado = estado;
        Dias = dias;
        FechaInicio = fechaInicio;
        FechaFin = fechaInicio.AddDays(dias).Date;
    }

    public static Licencia Crear(Articulo articulo, int dias, DateTime fechaInicio, string observacion)
    {
        return new(articulo, Estado.Pendiente, dias, fechaInicio, observacion);
    }

    public Licencia ActualizarLicencia(Articulo articulo, int dias, DateTime fechaInicio, string observacion)
    {
        return Crear(articulo, dias, fechaInicio, observacion);
    }

    public Licencia Activar() => new(Articulo, Estado.Activa, Dias, FechaInicio, Observacion);

    public Licencia Cancelar(string observacion) => new(Articulo, Estado.Cancelada, Dias, FechaInicio, observacion);

    public Licencia Finalizar() => new(Articulo, Estado.Finalizada, Dias, FechaInicio, Observacion);

    public Licencia ExtenderLicencia(int dias)
    {
        if (dias <= 0)
        {
            throw new ArgumentException("Se debe especificar la cantidad de días que quiere extender la licencia.");
        }

        return new(Articulo, Estado, Dias + dias, FechaInicio, Observacion);
    }

    public Licencia CambiarObservacion(string observacion)
    {
        return new(Articulo, Estado, Dias, FechaInicio, observacion);
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Articulo;
        yield return Estado;
        yield return Dias;
        yield return FechaInicio;
        yield return FechaFin;
        yield return Observacion;
    }
}
