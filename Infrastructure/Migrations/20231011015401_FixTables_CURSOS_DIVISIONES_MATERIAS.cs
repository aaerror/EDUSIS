using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTables_CURSOS_DIVISIONES_MATERIAS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_materias_descripcion",
                table: "materias");

            migrationBuilder.DropIndex(
                name: "IX_divisiones_descripicion",
                table: "divisiones");

            migrationBuilder.DropIndex(
                name: "IX_cursos_descripcion",
                table: "cursos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_materias_descripcion",
                table: "materias",
                column: "descripcion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_divisiones_descripicion",
                table: "divisiones",
                column: "descripicion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cursos_descripcion",
                table: "cursos",
                column: "descripcion",
                unique: true);
        }
    }
}
