using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuitarClaveAjenaPreceptorDePlanilla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PLANILLA_ASISTENCIA_DOCENTE",
                table: "planilla_asistencia");

            migrationBuilder.DropIndex(
                name: "IX_planilla_asistencia_preceptor_id",
                table: "planilla_asistencia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_planilla_asistencia_preceptor_id",
                table: "planilla_asistencia",
                column: "preceptor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_PLANILLA_ASISTENCIA_DOCENTE",
                table: "planilla_asistencia",
                column: "preceptor_id",
                principalTable: "docente",
                principalColumn: "persona_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
