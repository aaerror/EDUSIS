namespace Core.ServicioMaterias.DTOs.Requests;

public record ModificarMateriaRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }
    public string Descripcion { get; init; }
    public int HorasCatedra { get; init; }


    public ModificarMateriaRequest(Guid cursoID, Guid materiaID, string descripcion, int horasCatedra)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
    }
}