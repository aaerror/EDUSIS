using Domain.Personas.Exceptions;

namespace Domain.Personas.Domicilios;

public class Direccion
{
    public string Calle { get; private set; }
    public int Altura { get; private set; }
    public Vivienda Vivienda { get; private set; }
    public string Observacion { get; private set; } = string.Empty;



    private Direccion(string calle, int altura, Vivienda vivienda, string observacion)
    {
        if (string.IsNullOrWhiteSpace(calle))
        {
            throw new ArgumentNullException(nameof(calle), $"La dirección posee datos incompletos.");
        }

        if (!string.IsNullOrWhiteSpace(observacion))
        {
            if (observacion.Trim().Length > 120)
            {
                throw new ExcesoCaracteresException("La observación no debe superar los 120 caracteres.");
            }

            Observacion = observacion.Trim();
        }

        Vivienda = vivienda;
        Calle = calle.Trim();
        Altura = altura;
        Observacion = observacion.Trim();
    }

    public static Direccion Crear(string calle, int altura, int vivienda, string observacion)
    {
        return new(calle, altura, (Vivienda) vivienda, observacion);
    }
}