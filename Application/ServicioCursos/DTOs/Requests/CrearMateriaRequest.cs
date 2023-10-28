namespace Core.ServicioCursos.DTOs.Requests;

public record CrearMateriaRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public int HorasCatedra {  get; set; }


    public CrearMateriaRequest(string descripcion, int horasCatedra)
    {
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
    }

}
