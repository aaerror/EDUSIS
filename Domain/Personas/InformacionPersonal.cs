using System.Text.RegularExpressions;

namespace Domain.Personas
{
    public class InformacionPersonal
    {
        public string Apellido { get; private set; }
        public string Nombre { get; private set; }
        public string Documento { get; private set; }
        public Sexo Sexo { get; private set; } = Sexo.EMPTY;
        public DateTime FechaNacimiento { get; private set; }
        public string Nacionalidad { get; private set; }


        private InformacionPersonal(string apellido, string nombre, string documento, Sexo sexo, DateTime fechaNacimiento, string nacionalidad)
        {
            var mensajeError = "La información personal posee datos incompletos.";
            // Corroboramos los datos que se ingresa
            if (string.IsNullOrWhiteSpace(apellido.Trim()))
            {
                throw new ArgumentNullException(nameof(apellido), mensajeError);
            }

            if (string.IsNullOrWhiteSpace(nombre.Trim()))
            {
                throw new ArgumentNullException(nameof(nombre), mensajeError);
            }

            if (string.IsNullOrWhiteSpace(documento.Trim()))
            {
                throw new ArgumentNullException(nameof(documento), mensajeError);
            }

            // Validamos el formato del documento ingresado.
            Regex re = new Regex(@"^(\d{7,8})$");
            if (!re.IsMatch(documento))
            {
                throw new FormatException($"El formato del DNI, { documento }, es inválido. Formatos habilitados: xxxxxxxx");
            }

            if (Sexo.Equals(sexo))
            {
                throw new ArgumentNullException(nameof(sexo), mensajeError);
            }
            /*
			string patron = string.Concat(@"^(2[0347]|3[034])\-?", $"({ dni })", @"\-?(\d)$");
			re = new Regex(patron);
			if (!re.IsMatch(cuil))
			{
				throw new FormatException($"El formato del CUIL, { CUIL }, es inválido. Formatos habilitados: xx-xxxxxxxx-x");
			}
			*/

            Apellido = apellido;
            Nombre = nombre;
            Documento = documento;
            Sexo = sexo;
            EstablecerFechaNacimiento(fechaNacimiento);
            Nacionalidad = nacionalidad;
        }

        public static InformacionPersonal Crear(string apellido, string nombre, string dni, int tipoSexo, DateTime fechaNacimiento, string nacionalidad)
        {
            var mensajeError = "La información personal posee datos incompletos.";

            if (tipoSexo < 0 || tipoSexo >= 2)
            {
                throw new ArgumentNullException(nameof(tipoSexo), mensajeError);
            }

            if (string.IsNullOrWhiteSpace(nacionalidad.Trim()))
            {
                throw new ArgumentNullException(nameof(nacionalidad), mensajeError);
            }

            return new(apellido, nombre, dni, (Sexo)tipoSexo, fechaNacimiento, nacionalidad);
        }

        private void EstablecerFechaNacimiento(DateTime fechaNacimiento)
        {
            if (fechaNacimiento.Date > DateTime.Today.Date)
            {
                throw new ArgumentException($"La fecha de nacimiento no puede ser mayor a la fecha actual ({DateTime.Today.Date}).", nameof(fechaNacimiento));
            }

            FechaNacimiento = fechaNacimiento.Date;
        }

        public string NombreCompleto() => $"{Apellido}, {Nombre}";

        public int Edad() => DateTime.Today.Year - FechaNacimiento.Date.Year;
    }
}
