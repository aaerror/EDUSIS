namespace Core.ServicioCursos.DTOs.Responses;

public record MateriaResponse
{
    public Guid Curso { get; set; }
    public Guid Materia { get; set; }
    public string Descripcion { get; set; }
    public int HorasCatedra { get; set; }
    public Guid? Profesor { get; set; }
    public string NombreCompletoProfesor { get; set; }
    public IReadOnlyCollection<SituacionRevistaResponse> Profesores { get; set; }
    public IReadOnlyCollection<HorarioResponse> Horarios { get; set; }


    public MateriaResponse(Guid curso, Guid materia, string descripcion, int horasCatedra, Guid? profesor, string nombreCompletoProfesor, List<SituacionRevistaResponse> profesores, List<HorarioResponse> horarios)
    {
        Curso = curso;
        Materia = materia;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
        Profesor = profesor;
        NombreCompletoProfesor = nombreCompletoProfesor;
        Profesores = profesores;
        Horarios = horarios;
    }
}
