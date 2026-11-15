namespace Domain.Shared;

/// <summary>
/// Punto único de lectura de la fecha actual para toda regla que dependa de "hoy".
/// </summary>
/// <remarks>
/// Es ambiental porque las entidades no reciben dependencias: pasarles un <see cref="IReloj"/>
/// cambiaría la firma de cada método con una regla de vigencia. Por defecto lee el reloj del
/// sistema; las pruebas lo fijan con <see cref="Fijar"/>.
/// <para>
/// El reloj fijado vive en un <see cref="AsyncLocal{T}"/> y no en un campo estático: un estático
/// compartido haría que dos pruebas en paralelo se pisaran la fecha. Con <c>AsyncLocal</c> cada
/// prueba lo ve sólo en su propio flujo de ejecución. Por eso hay que fijarlo dentro del método
/// de prueba (o de algo que éste llame) y no en el constructor de la clase de prueba, que no
/// garantiza compartir ese flujo.
/// </para>
/// </remarks>
public static class Reloj
{
	private static readonly IReloj _sistema = new RelojDelSistema();
	private static readonly AsyncLocal<IReloj?> _fijado = new();

	public static DateTime Hoy => (_fijado.Value ?? _sistema).Hoy;

	/// <summary>
	/// Reemplaza el reloj en el flujo actual hasta liberar el valor devuelto
	/// (<c>using var _ = Reloj.Fijar(...)</c>).
	/// </summary>
	public static IDisposable Fijar(IReloj reloj)
	{
		ArgumentNullException.ThrowIfNull(reloj);

		var anterior = _fijado.Value;
		_fijado.Value = reloj;

		return new Restauracion(anterior);
	}

	private sealed class RelojDelSistema : IReloj
	{
		public DateTime Hoy => DateTime.Today;
	}

	private sealed class Restauracion : IDisposable
	{
		private readonly IReloj? _anterior;
		private bool _liberado;

		public Restauracion(IReloj? anterior) => _anterior = anterior;

		public void Dispose()
		{
			if (_liberado)
			{
				return;
			}

			_fijado.Value = _anterior;
			_liberado = true;
		}
	}
}
