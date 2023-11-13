using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRelation_DOCENTE_SITUACIONREVISTA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista");

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista",
                column: "profesor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista");

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista",
                column: "profesor_id",
                unique: true);
        }
    }
}
