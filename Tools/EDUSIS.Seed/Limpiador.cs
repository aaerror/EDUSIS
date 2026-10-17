using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EDUSIS.Seed;

/// <summary>
/// Borra los datos de demo dejando el esquema y los datos de referencia, para que el seeder se
/// pueda volver a correr sobre una base ya sembrada.
/// </summary>
/// <remarks>
/// <para>
/// Las tablas se borran <strong>hijos antes que padres</strong> y en una transacción: un orden
/// mal puesto rompe contra una FK a mitad de camino y deja la base en un estado peor que el
/// inicial. <c>rol</c> y <c>ciclo_lectivo</c> quedan intactas porque son datos de referencia
/// sembrados por <c>HasData</c> en la migración: son parte del esquema, no del dataset.
/// </para>
/// <para>
/// Es exactamente el error que la suite de integración tiene abierto como TEST-09:
/// <c>SqlServerFixture</c> inicializa <c>Respawner</c> ignorando sólo
/// <c>__EFMigrationsHistory</c>, así que entre pruebas borra también los datos de referencia y
/// doce pruebas fallan contra <c>FK_CURSANTE_CICLO_LECTIVO</c>. Acá la lista es explícita para
/// no repetirlo.
/// </para>
/// </remarks>
internal static class Limpiador
{
	/// <summary>
	/// Las 19 tablas de datos, de hoja a raíz. Faltan <c>rol</c> y <c>ciclo_lectivo</c> a
	/// propósito. <c>domicilio</c> es la tabla del value object propiedad de <c>persona</c>
	/// (<c>OwnsOne</c>), y <c>alumno</c>/<c>docente</c> son las hojas TPT de <c>persona</c>: las
	/// tres van antes que ella.
	/// </summary>
	private static readonly string[] TABLAS_EN_ORDEN_DE_BORRADO =
	{
		"registro_asistencia",
		"planilla_asistencia",
		"calificacion",
		"cursante",
		"horario",
		"situacion_revista",
		"catedra",
		"materia",
		"diseno_curricular",
		"division",
		"curso",
		"licencia",
		"usuario_rol",
		"usuario",
		"puesto",
		"domicilio",
		"alumno",
		"docente",
		"persona"
	};

	internal static async Task LimpiarAsync(EdusisDBContext contexto, ILogger registrador)
	{
		await using var transaccion = await contexto.Database.BeginTransactionAsync();

		await RomperElCicloDeClavesForaneasAsync(contexto, registrador);

		var borradas = 0;

		foreach (var tabla in TABLAS_EN_ORDEN_DE_BORRADO)
		{
			// Nombres de tabla de una constante del propio código, nunca de la entrada: no hay
			// dato externo que pueda llegar a esta cadena.
			var filas = await contexto.Database.ExecuteSqlRawAsync($"DELETE FROM [{tabla}]");

			if (filas > 0)
			{
				registrador.LogInformation("  {Tabla}: {Filas} fila/s borrada/s.", tabla, filas);
				borradas += filas;
			}
		}

		await transaccion.CommitAsync();
		registrador.LogInformation("Base limpia: {Total} filas borradas, datos de referencia intactos.", borradas);
	}

	/// <summary>
	/// Pone en NULL <c>catedra.situacion_en_funciones_id</c> antes de empezar a borrar.
	/// </summary>
	/// <remarks>
	/// <c>catedra</c> y <c>situacion_revista</c> se referencian <strong>mutuamente</strong>:
	/// <c>FK_CATEDRA_SITUACION_REVISTA</c> va de la cátedra a su situación en funciones y
	/// <c>FK_SITUACION_REVISTA_CATEDRA</c> vuelve en el otro sentido. Con un ciclo así
	/// <strong>no existe ningún orden de borrado que funcione</strong>: borrar la situación
	/// primero viola la FK de la cátedra, y borrar la cátedra primero viola la de la situación.
	/// <para>
	/// La salida es que la columna es nullable, que es justamente lo que el dominio modela: una
	/// cátedra puede no tener docente en funciones (<c>Catedra.RelevarDeFunciones</c>). Anularla
	/// corta el ciclo y recién entonces el orden de hoja a raíz alcanza. Es la contracara del
	/// mismo problema que obliga al seeder a guardar las cátedras en dos pasos.
	/// </para>
	/// </remarks>
	private static async Task RomperElCicloDeClavesForaneasAsync(EdusisDBContext contexto, ILogger registrador)
	{
		var afectadas = await contexto.Database.ExecuteSqlRawAsync(
			"UPDATE [catedra] SET [situacion_en_funciones_id] = NULL WHERE [situacion_en_funciones_id] IS NOT NULL");

		if (afectadas > 0)
		{
			registrador.LogInformation(
				"  catedra: {Filas} docente/s en funciones desvinculado/s para poder borrar (ciclo de FK).",
				afectadas);
		}
	}
}
