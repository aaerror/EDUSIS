namespace Core.ServicioMaterias.DTOs.Responses;

public record MateriaResponse
{
    public Guid CursoID { get; set; }
    public Guid MateriaID { get; set; }
    public string Descripcion { get; set; }
    public int HorasCatedra { get; set; }
    public int HorasCatedraSinAsignar { get; set; }
    public Guid? ProfesorID { get; set; }
    public string NombreCompletoProfesor { get; set; }


    public MateriaResponse(Guid cursoID, Guid materiaID, string descripcion, int horasCatedra, int horasCatedraSinAsignar, Guid? profesorID, string nombreCompletoProfesor)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        Descripcion = descripcion;
        HorasCatedra = horasCatedra;
        HorasCatedraSinAsignar = horasCatedraSinAsignar;
        ProfesorID = profesorID;
        NombreCompletoProfesor = nombreCompletoProfesor;
    }
}
