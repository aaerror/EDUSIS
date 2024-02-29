using Domain.Personas.Domicilios;
using Domain.Personas.Exceptions;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Personas;

public abstract class Persona : Entity
{
    public InformacionPersonal InformacionPersonal { get; private set; }
    public Domicilio Domicilio { get; private set; }
    public string Email { get; private set; }
    public string Telefono { get; private set; }


    protected Persona()
        : base() { }

    protected Persona(Guid personaId)
        : base(personaId) {}

    protected Persona(Guid personaId, InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono)
        : this(personaId)
    {
        Id = personaId;
        InformacionPersonal = informacionPersonal;
        Domicilio = domicilio;
        CargarEmail(email);
        CargarTelefono(telefono);
    }

    public Persona(InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono)
        : this(Guid.NewGuid(), informacionPersonal, domicilio, email, telefono)
    {
        if (informacionPersonal is null)
        {
            throw new ArgumentNullException(nameof(informacionPersonal), "Datos personales inexistentes.");
        }

        if (domicilio is null)
        {
            throw new ArgumentNullException(nameof(domicilio), "Domicilio inexistentes.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email), "Email inexistente.");
        }


        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new ArgumentNullException(nameof(telefono), "Telefono inexistente.");
        }
    }

    #region InformacionPersonal
    public void CambiarNombreCompleto(string apellido, string nombre)
    {
        InformacionPersonal = InformacionPersonal.CambiarNombreCompleto(apellido, nombre);
    }

    public void CambiarSexo(string apellido, string nombre, int sexo)
    {
        InformacionPersonal = InformacionPersonal.CambiarSexo(apellido, nombre, sexo);
    }
    #endregion

    #region Domicilio
    public void CambiarDomicilio(Domicilio nuevoDomicilio)
    {
        if (nuevoDomicilio == null)
        {
            throw new ArgumentNullException(nameof(nuevoDomicilio), "Datos del nuevo domicilio incompletos.");
        }

        Domicilio = nuevoDomicilio;
    }

    public void CambiarDireccion(Direccion nuevaDireccion)
    {
        Domicilio = Domicilio.CambiarDireccion(nuevaDireccion);
    }
    #endregion

    #region Contacto
    private bool EsEmailValido(string unEmail)
    {
        bool esValido = false;

        /**
         * Usamos regular expression para validar el formato del email.
         * https://mailtrap.io/blog/validate-email-address-c/
         */
        Regex re = new Regex(@"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$");
        if (re.IsMatch(unEmail))
        {
            esValido = true;
        }

        return esValido;
    }

    private void CargarEmail(string email)
    {
        if (!EsEmailValido(email))
        {
            throw new FormatoInvalidoException(email);
        }

        Email = email;
    }

    private bool EsTelefonoValido(string unTelefono)
    {
        bool esValido = false;

        /**
         * Usamos regular expression para validar el formato del número de teléfono
         * 
         * https://es.stackoverflow.com/questions/136325/validar-tel%C3%A9fonos-de-argentina-con-una-expresi%C3%B3n-regular
         * 
         * Toma como opcionales:
         *      el prefijo internacional (54)
         *      el prefijo internacional para celulares (9)
         *      el prefijo de acceso a interurbanas (0)
         *      el prefijo local para celulares (15)
         * Es obligatorio:
         *       el código de área (11, 2xx, 2xxx, 3xx, 3xxx, 6xx y 8xx)
         *       (no toma como válido un número local sin código de área como 4444-0000)
         **/
        Regex re = new Regex(@"^(?:(?:00)?549?)?0?(?:11|[2368]\d)(?:(?=\d{0,2}15)\d{2})??\d{8}$");
        if (re.IsMatch(unTelefono))
        {
            esValido = true;
        }

        return esValido;
    }

    private void CargarTelefono(string telefono)
    {
        if (!EsTelefonoValido(telefono))
        {
            throw new FormatoInvalidoException(telefono);
        }

        Telefono = telefono;
    }

    public void CambiarContacto(string unEmail, string unTelefono)
    {
        CargarEmail(unEmail);
        CargarTelefono(unTelefono);
    }
    #endregion
}