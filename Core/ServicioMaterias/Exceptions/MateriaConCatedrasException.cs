namespace Core.ServicioMaterias.Exceptions;

public class MateriaConCatedrasException : Exception
{
	private const string ERROR = "No se puede modificar la carga horaria de una materia que ya tiene cátedras registradas.";


	public MateriaConCatedrasException() : base(ERROR) { }
}
