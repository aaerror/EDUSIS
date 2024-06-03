namespace Core.ServicioCursos.DTOs.Requests;

public record RegistrarCursoRequest
{
    public string Descripcion { get; init; }
    public int NivelEducativo { get; init; }


    public RegistrarCursoRequest(string descripcion, int nivelEducativo)
    {
        Descripcion = descripcion;
        NivelEducativo = nivelEducativo;
    }
}
