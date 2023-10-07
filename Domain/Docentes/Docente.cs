using Domain.Personas;
using Domain.Personas.Domicilios;
using System.Text.RegularExpressions;

namespace Domain.Docentes;

public abstract class Docente : Persona
{
    private List<Puesto> _puestos;

    public string Legajo { get; private set; }
    public string CUIL { get; private set; }
    public DateTime FechaAltaInstitucion { get; private set; }
    public DateTime FechaBajaInstitucion { get; private set; }
    public bool EstaActivo { get; private set; }
    public IReadOnlyCollection<Puesto> Puestos { get { return _puestos.AsReadOnly(); } }


    protected Docente()
        : base() { }

    protected Docente(string legajo, string cuil, InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono) 
        : base(informacionPersonal, domicilio, email, telefono)
    {
        _puestos = new List<Puesto>();
        ValidarLegajo(legajo);
        ValidarCuil(cuil);

        Legajo = legajo;
        CUIL = cuil;
    }

    private void ValidarLegajo(string legajoDocente)
    {
        if (!Regex.IsMatch(legajoDocente, @"^(\d{6})$"))
        {
            throw new FormatException($"Verificar el legajo del docente: { nameof(legajoDocente) }.");
        }
    }

    private void ValidarCuil(string cuil)
    {
        //@"^\d{2}-\d{8}-\d{1}$
        if (!Regex.IsMatch(cuil, @"\b(20|23|24|27|30|33|34)(\D)?[0-9]{8}(\D)?[0-9]"))
        {
            throw new FormatException($"Verificar el CUIL del docente: { nameof(cuil) }.");
        }
    }

    public void AgregarPuesto(Posicicion cargo, DateTime fechaAlta, DateTime fechaBaja)
    {
        _puestos.Add(Puesto.Create(cargo, fechaAlta, fechaBaja));
    }
}
