namespace Domain.Personas;

public class Contacto
{
    public TipoContacto TipoContacto { get; private set; }
    public string Descripcion { get; private set; }


    public Contacto(TipoContacto tipoContacto, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion.Trim()))
        {
            throw new ArgumentNullException(nameof(descripcion), "Datos de contacto sin especificar.");
        }

       /* if (TipoContacto.Equals(tipoContacto))
        {
            throw new ArgumentNullException(nameof(tipoContacto), "Datos de contacto sin especificar.");
        }*/

        TipoContacto = tipoContacto;
        Descripcion = descripcion.Trim();
    }

    public static Contacto Crear(int tipoContacto, string descripcion)
    {

        return new((TipoContacto) tipoContacto, descripcion);
    }

    public Contacto ModificarContacto(string descripcion)
    {
        return new(TipoContacto, descripcion);
    }
}
