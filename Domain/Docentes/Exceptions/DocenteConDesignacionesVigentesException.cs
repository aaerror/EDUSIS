namespace Domain.Docentes.Exceptions;

public class DocenteConDesignacionesVigentesException : Exception
{
	private const string ERROR = "El docente tiene designaciones vigentes en cátedras y no se puede eliminar. Cerrar o reemplazar esas designaciones primero.";


	public DocenteConDesignacionesVigentesException()
		: base(ERROR) { }
	public DocenteConDesignacionesVigentesException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteConDesignacionesVigentesException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
