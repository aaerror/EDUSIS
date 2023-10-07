namespace Core.ServicioCursos.DTOs.Requests;

public record MateriaRequest
{
    public string Descripcion { get; set; }
    public int HorasCatedra {  get; set; }
}
