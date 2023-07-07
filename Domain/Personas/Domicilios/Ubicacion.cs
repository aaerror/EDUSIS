using System.Text.RegularExpressions;

namespace Domain.Personas.Domicilios
{
    public class Ubicacion
    {
        public string Ciudad { get; private set; }
        public string Provincia { get; private set; }
        public string CodigoPostal { get; private set; }
        public string Pais { get; private set; } = "Argentina";


        private Ubicacion(string ciudad, string provincia, string codigoPostal, string pais)
        {
            // Comprobamos datos nulos o incompletos
            if (string.IsNullOrWhiteSpace(ciudad))
            {
                throw new ArgumentNullException(nameof(ciudad), "La ubicación posee datos incompletos.");
            }

            if (string.IsNullOrWhiteSpace(provincia))
            {
                throw new ArgumentNullException(nameof(provincia), "La ubicación posee datos incompletos.");
            }

            if (string.IsNullOrWhiteSpace(codigoPostal))
            {
                throw new ArgumentNullException(nameof(codigoPostal), "La ubicación posee datos incompletos.");
            }

            if (string.IsNullOrWhiteSpace(pais))
            {
                throw new ArgumentNullException(nameof(pais), "La ubicación posee datos incompletos.");
            }

            // Validamos que se ingresen letras y no números
            string regularExpression = @"^([A-Z]{1}[a-zñáéíóúü]+[\s]?)+$";
            Regex re = new(regularExpression);
            if (!re.IsMatch(ciudad.Trim()))
            {
                throw new FormatException($"La ciudad {ciudad} debe ingresarse como un nombre propio.");
            }

            if (!re.IsMatch(provincia.Trim()))
            {
                throw new FormatException($"La provincia {provincia} debe ingresarse como un nombre propio.");
            }

            if (!re.IsMatch(pais.Trim()))
            {
                throw new FormatException($"El país {pais} debe ingresarse como un nombre propio.");
            }

            regularExpression = @"^(\d{4})$";
            re = new Regex(regularExpression);
            if (!re.IsMatch(codigoPostal.Trim()))
            {
                throw new FormatException($"El codigo postal {codigoPostal} debe contener 4 digitos.");
            }

            Ciudad = ciudad.Trim();
            Provincia = provincia.Trim();
            CodigoPostal = codigoPostal.Trim();
            Pais = pais.Trim();
        }

        public static Ubicacion Crear(string ciudad, string provincia, string codigoPostal, string pais)
        {
            return new(ciudad, provincia, codigoPostal, pais);
        }
    }
}