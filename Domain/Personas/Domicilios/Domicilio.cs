namespace Domain.Personas.Domicilios;

public class Domicilio
{
    public Direccion Direccion { get; private set; }
    public Ubicacion Ubicacion { get; private set; }


    private Domicilio() { }

    private Domicilio(string calle, int altura, int vivienda, string observacion, string localidad, string provincia, string pais)
    {
        Direccion = Direccion.Crear(calle, altura, vivienda, observacion);
        Ubicacion = Ubicacion.Crear(localidad, provincia, pais);
    }

    private Domicilio(Direccion direccion, Ubicacion ubicacion)
    {
        if (direccion is null)
        {
            throw new ArgumentNullException(nameof(direccion), "Dirección del domicilio inexistente.");
        }

        if (ubicacion is null)
        {
            throw new ArgumentNullException(nameof(ubicacion), "Ubicación del domicilio inexistente.");
        }

        Direccion = direccion;
        Ubicacion = ubicacion;
    }

    public static Domicilio Crear(string calle, int altura, int vivienda, string observacion, string localidad, string provincia, string pais)
    {
        return new(calle, altura, vivienda, observacion, localidad, provincia, pais);
    }

    public Domicilio CambiarDireccion(string calle, int altura, int tipoVivienda, string observacion)
    {
        Direccion nuevaDireccion = Direccion.Crear(calle, altura, tipoVivienda, observacion);
        return new(nuevaDireccion, Ubicacion);
    }

    public Domicilio ModificarObservacionDomicilio(string observacion)
    {
        Direccion nuevaDireccion = Direccion.Crear(Direccion.Calle, Direccion.Altura, (int) Direccion.Vivienda, observacion);
        return new(nuevaDireccion, Ubicacion);
    }
}