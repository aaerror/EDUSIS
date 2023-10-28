namespace Core.ServicioCursos.DTOs.Requests;

public record CrearCursoRequest
{
    public string Descripcion { get; set; }
    public int NivelEducativo { get; set; }


    public CrearCursoRequest(string descripcion, int nivelEducativo)
    {
        Descripcion = descripcion;
        NivelEducativo = nivelEducativo;
    }
}
