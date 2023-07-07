using Domain.Personas.Domicilios;

namespace Domain.Personas
{
    public abstract class Persona
    {
        private List<Contacto> _contactos;
        public string PersonaId { get; private set; }

        public InformacionPersonal InformacionPersonal { get; private set; }

        public Domicilio Domicilio { get; private set; }

        public IReadOnlyCollection<Contacto> Contactos => _contactos.AsReadOnly();


        public Persona(InformacionPersonal informacionPersonal, Domicilio unDomicilio)
        {
            _contactos = new List<Contacto>();

            if (informacionPersonal is null)
            {
                throw new ArgumentNullException(nameof(informacionPersonal), "Información personal inexistente.");
            }

            if (unDomicilio is null)
            {
                throw new ArgumentNullException(nameof(unDomicilio), "Domicilio inexistente.");
            }

            InformacionPersonal = informacionPersonal;
            Domicilio = unDomicilio;
        }

    }
}