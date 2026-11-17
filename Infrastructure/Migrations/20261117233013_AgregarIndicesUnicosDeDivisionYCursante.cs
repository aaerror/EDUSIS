using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndicesUnicosDeDivisionYCursante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_division_curso_id",
                table: "division");

            migrationBuilder.CreateIndex(
                name: "IX_division_curso_id_descripcion",
                table: "division",
                columns: new[] { "curso_id", "descripcion" },
                unique: true);

            // Fuera del modelo: EF Core no admite un HasIndex que mezcle AlumnoID (dueño) con
            // CicloLectivo.Periodo (tipo poseído). Ver CursantesConfiguration. Filtrado por
            // inscripción activa: el traslado de división cierra una y abre otra en el mismo ciclo.
            migrationBuilder.Sql(@"
                CREATE UNIQUE NONCLUSTERED INDEX UQ_CURSANTE_ALUMNO_CICLO_ACTIVA
                ON cursante (alumno_id, ciclo_lectivo)
                WHERE fecha_fin IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX UQ_CURSANTE_ALUMNO_CICLO_ACTIVA ON cursante;");

            migrationBuilder.DropIndex(
                name: "IX_division_curso_id_descripcion",
                table: "division");

            migrationBuilder.CreateIndex(
                name: "IX_division_curso_id",
                table: "division",
                column: "curso_id");
        }
    }
}
