using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarArticulosDeLicenciaSegunNormativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "articulo",
                table: "licencia",
                type: "varchar(40)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(15)");

            // Los cinco artículos viejos pasan a su equivalente del catálogo legal. "Particular"
            // es la licencia por otras causas del art. 19 y "ART", la del accidente de trabajo.
            migrationBuilder.Sql(@"
                UPDATE licencia SET articulo = CASE articulo
                    WHEN 'Enfermedad' THEN 'EnfermedadCortoTratamiento'
                    WHEN 'Matrimonio' THEN 'MatrimonioPropio'
                    WHEN 'Particular' THEN 'OtrasCausas'
                    WHEN 'ART'        THEN 'AccidenteDeTrabajo'
                    ELSE articulo
                END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La vuelta pierde información: los artículos que no existían antes no tienen
            // equivalente y caen en "Particular", el comodín del enumerado viejo. Va antes de
            // achicar la columna porque ningún nombre nuevo entra en varchar(15).
            migrationBuilder.Sql(@"
                UPDATE licencia SET articulo = CASE articulo
                    WHEN 'EnfermedadCortoTratamiento' THEN 'Enfermedad'
                    WHEN 'EnfermedadLargoTratamiento' THEN 'Enfermedad'
                    WHEN 'MatrimonioPropio'           THEN 'Matrimonio'
                    WHEN 'Maternidad'                 THEN 'Maternidad'
                    WHEN 'AccidenteDeTrabajo'         THEN 'ART'
                    ELSE 'Particular'
                END;");

            migrationBuilder.AlterColumn<string>(
                name: "articulo",
                table: "licencia",
                type: "varchar(15)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)");
        }
    }
}
