using Domain.Personas;
using Domain.Personas.Domicilios;

namespace Domain.Alumno;

public class Alumno : Persona
{
    public string Legajo { get; private set; }

    private Alumno() : base() { }

    private Alumno(InformacionPersonal informacionPersonal, Domicilio domicilio) : base(informacionPersonal, domicilio) { }

    public Alumno(string legajo, InformacionPersonal informacionPersonal, Domicilio domicilio) : this(informacionPersonal, domicilio)
    {
        Legajo = legajo;
    }
}
