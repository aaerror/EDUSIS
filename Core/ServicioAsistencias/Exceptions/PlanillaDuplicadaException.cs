namespace Core.ServicioAsistencias.Exceptions;

public class PlanillaDuplicadaException : Exception
{
	private const string ERROR = "Ya existe una planilla de asistencia para esa división en esa fecha.";


	public PlanillaDuplicadaException() : base(ERROR) { }
}
