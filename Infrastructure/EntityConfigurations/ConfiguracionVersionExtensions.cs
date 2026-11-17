using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

/// <summary>
/// Concurrencia optimista por raíz de agregado: una columna <c>rowversion</c> que SQL Server
/// cambia en cada UPDATE y que EF agrega al WHERE de cada UPDATE/DELETE de la fila.
/// </summary>
/// <remarks>
/// <para>
/// Es una propiedad SOMBRA: el dominio no sabe que existe. EF guarda el valor leído en el
/// <c>ChangeTracker</c> y lo compara al guardar; si otra estación modificó la fila en el medio,
/// <c>SaveChanges</c> no afecta ninguna fila y lanza <c>DbUpdateConcurrencyException</c>, que
/// <c>UnitOfWork.GuardarCambiosAsync</c> traduce a <c>ConflictoDeConcurrenciaException</c>.
/// </para>
/// <para>
/// Sólo va en las raíces. Las entidades internas del agregado (calificaciones, horarios,
/// situaciones de revista, registros de asistencia, puestos, roles del usuario) no la llevan:
/// cuando cambian, <c>UnitOfWork</c> marca su raíz como modificada para que el UPDATE de la raíz
/// verifique y avance la versión del agregado entero.
/// </para>
/// </remarks>
internal static class ConfiguracionVersionExtensions
{
	public const string PROPIEDAD = "Version";
	public const string COLUMNA = "version";

	public static void ConfigurarVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class
	{
		builder.Property<byte[]>(PROPIEDAD)
			   .HasColumnName(COLUMNA)
			   .IsRowVersion();
	}
}
