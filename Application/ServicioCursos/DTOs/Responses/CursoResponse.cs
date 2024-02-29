namespace Core.ServicioCursos.DTOs.Responses;

public record CursoResponse
{
    public Guid CursoID { get; set; }
    public string Descripcion { get; set; }
    public string NivelEducativo { get; set; }
    public int Divisiones {  get; set; }
    public int Materias { get; set; }
    public int Alumnos { get; set; }
}