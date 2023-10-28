namespace Core.ServicioCursos.DTOs.Requests;

public record EditarMateriaRequest
{
    public Guid Curso { get; set; }
    public Guid Materia { get; set; }
    public string Descripcion { get; set; }
    public int HorasCatedra { get; set; }


    public EditarMateriaRequest(Guid curso, Guid materia, string descripcion, int horasCatedra)
    {
        Curso = curso;
        Materia = materia;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
    }
}
