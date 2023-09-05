using Domain.Personas;
using Domain.Personas.Domicilios;

namespace Domain.PersonalDocente;

public class PersonalDocente : Persona
{
    public string Legajo { get; private set; }
    public string CUIL { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime FechaBaja { get; private set; }
    public PuestoDocente Rol { get; private set; }


    protected PersonalDocente() { }

    private PersonalDocente(InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono) 
        : base(informacionPersonal, domicilio, email, telefono)
    { }

    private PersonalDocente(string legajo, string cuil, InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono) : this(informacionPersonal, domicilio, email, telefono)
    {
        Legajo = legajo;
        CUIL = cuil;
    }

}
