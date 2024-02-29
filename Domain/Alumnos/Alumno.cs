using Domain.Personas;
using Domain.Personas.Domicilios;

namespace Domain.Alumnos;

public class Alumno : Persona
{
    public string Legajo { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; }


    private Alumno()
        : base() { }

    public Alumno(string legajo, InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono)
        : base(informacionPersonal, domicilio, email, telefono)
    {
        Legajo = legajo;
        FechaAlta = DateTime.Today;
    }
}
