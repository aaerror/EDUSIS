using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// La columna <c>instancia</c> —"Parcial" o "Recuperatorio"— se reparte en <c>periodo</c>, el
    /// tramo del ciclo lectivo, y <c>es_recuperatorio</c>, el segundo intento dentro de ese tramo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// EF la generó como <c>DropColumn</c> + dos <c>AddColumn</c>, que borra las calificaciones
    /// cargadas. Está reescrita a mano para preservarlas: primero agrega, después traduce con SQL y
    /// recién entonces borra la vieja.
    /// </para>
    /// <para>
    /// LA TRADUCCIÓN PIERDE PRECISIÓN Y NO HAY FORMA DE EVITARLO: el modelo anterior no registraba a
    /// qué cuatrimestre pertenecía una nota, así que todo lo existente queda en el PRIMER
    /// cuatrimestre. El dato no estaba y la migración no puede inventarlo; lo que sí se conserva es
    /// la nota, la fecha y la distinción entre nota y recuperatorio. Las calificaciones viejas
    /// pueden necesitar una corrección manual del período.
    /// </para>
    /// </remarks>
    public partial class ReemplazarInstanciaPorPeriodoEnCalificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable al nacer: las filas que ya existen todavía no tienen valor, y un NOT NULL con
            // default las dejaría todas en cadena vacía antes de poder traducirlas.
            migrationBuilder.AddColumn<string>(
                name: "periodo",
                table: "calificacion",
                type: "varchar(30)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_recuperatorio",
                table: "calificacion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE calificacion
                   SET periodo = 'PrimerCuatrimestre',
                       es_recuperatorio = CASE WHEN instancia = 'Recuperatorio' THEN 1 ELSE 0 END;");

            migrationBuilder.AlterColumn<string>(
                name: "periodo",
                table: "calificacion",
                type: "varchar(30)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "instancia",
                table: "calificacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "instancia",
                table: "calificacion",
                type: "varchar(20)",
                nullable: true);

            // La vuelta atrás también pierde: los cuatro recuperatorios posibles de un ciclo
            // colapsan en un único "Recuperatorio", y la nota final y las recuperaciones quedan como
            // "Parcial" porque el modelo viejo no tiene cómo nombrarlas.
            migrationBuilder.Sql(@"
                UPDATE calificacion
                   SET instancia = CASE WHEN es_recuperatorio = 1 THEN 'Recuperatorio' ELSE 'Parcial' END;");

            migrationBuilder.AlterColumn<string>(
                name: "instancia",
                table: "calificacion",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "es_recuperatorio",
                table: "calificacion");

            migrationBuilder.DropColumn(
                name: "periodo",
                table: "calificacion");
        }
    }
}
