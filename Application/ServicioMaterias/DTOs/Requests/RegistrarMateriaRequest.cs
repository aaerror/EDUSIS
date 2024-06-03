namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarMateriaRequest
{
    public Guid CursoID { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int HorasCatedra {  get; set; }


    public RegistrarMateriaRequest(Guid cursoID, string descripcion, int horasCatedra)
    {
        CursoID = cursoID;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
    }
}
