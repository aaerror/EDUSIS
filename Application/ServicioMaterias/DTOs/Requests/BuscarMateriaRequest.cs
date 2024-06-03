namespace Core.ServicioMaterias.DTOs.Requests;

public class BuscarMateriaRequest
{
    public Guid MateriaID { get; init; }


    public BuscarMateriaRequest(Guid materiaID)
    {
        MateriaID = materiaID;
    }
}
