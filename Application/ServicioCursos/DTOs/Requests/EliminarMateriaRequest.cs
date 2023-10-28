namespace Core.ServicioCursos.DTOs.Requests;

public record EliminarMateriaRequest
{
    public Guid Curso { get; init; }
    public Guid Materia { get; init; }


    public EliminarMateriaRequest(Guid unCurso, Guid unaMateria)
    {
        Curso = unCurso;
        Materia = unaMateria;
    }
}
