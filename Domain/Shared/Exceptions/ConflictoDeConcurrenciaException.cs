namespace Domain.Shared.Exceptions;

/// <summary>
/// Otra estación guardó cambios sobre el mismo agregado entre que esta pantalla lo leyó y lo
/// guardó. Lo lanza <c>IUnitOfWork.GuardarCambiosAsync</c>; nada de lo de esta operación se
/// guardó, y para reintentar hay que volver a leer los datos.
/// </summary>
public class ConflictoDeConcurrenciaException : Exception
{
	private const string ERROR = "Otro usuario modificó estos datos mientras los editabas. No se guardó ningún cambio: volvé a abrir la pantalla para ver la versión actual.";


	public ConflictoDeConcurrenciaException()
		: base(ERROR) { }

	public ConflictoDeConcurrenciaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public ConflictoDeConcurrenciaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
