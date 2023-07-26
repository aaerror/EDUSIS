using Domain.Personas.Domicilios;
using Domain.Personas.Exceptions;
using Domain.Shared;

namespace Domain.Personas
{
    public abstract class Persona : Entity
    {
        private List<Contacto> _contactos;
        public InformacionPersonal InformacionPersonal { get; private set; }
        public Domicilio Domicilio { get; private set; }
        public IReadOnlyCollection<Contacto> Contactos => _contactos.AsReadOnly();

        protected Persona() : base() { } 

        protected Persona(Guid personaId) : base(personaId)
        {
            _contactos = new List<Contacto>();
        }

        public Persona(Guid personaId, InformacionPersonal informacionPersonal, Domicilio domicilio) : this(personaId)
        {
            Id = personaId;
            InformacionPersonal = informacionPersonal;
            Domicilio = domicilio;
        }

        // TODO: Chequear lista de contactos
        public Persona(InformacionPersonal informacionPersonal, Domicilio domicilio) : this(Guid.NewGuid(), informacionPersonal, domicilio)
        {
            if (informacionPersonal is null)
            {
                throw new ArgumentNullException(nameof(informacionPersonal), "Información personal inexistente.");
            }

            if (domicilio is null)
            {
                throw new ArgumentNullException(nameof(domicilio), "Domicilio inexistente.");
            }
        }

        #region Contacto
        private bool ExisteContacto(Contacto contactoBuscado)
        {
            if (contactoBuscado == null)
            {
                throw new ArgumentNullException(nameof(contactoBuscado), "Datos del contacto inexistentes.");
            }
            return _contactos.Contains(contactoBuscado); 
        }

        public void AgregarContacto(Contacto nuevoContacto)
        {
            if (ExisteContacto(nuevoContacto))
            {
                throw new ContactoDuplicadoException(nameof(nuevoContacto));
            }

            _contactos.Add(nuevoContacto);
        }

        public void EliminarContacto(Contacto unContacto)
        {
            if (!ExisteContacto(unContacto))
            {
                throw new ArgumentException("Contacto no registrado.", nameof(unContacto));
            }

            _contactos.Remove(unContacto);
        }
        #endregion
    }
}