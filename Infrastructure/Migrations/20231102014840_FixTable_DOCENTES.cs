using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTable_DOCENTES : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "fecha_baja",
                table: "puestos",
                newName: "fecha_fin");

            migrationBuilder.RenameColumn(
                name: "fecha_alta",
                table: "puestos",
                newName: "fecha_inicio");

            migrationBuilder.RenameColumn(
                name: "fecha_baja_institucion",
                table: "docentes",
                newName: "fecha_baja");

            migrationBuilder.RenameColumn(
                name: "fecha_alta_institucion",
                table: "docentes",
                newName: "fecha_alta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "fecha_inicio",
                table: "puestos",
                newName: "fecha_alta");

            migrationBuilder.RenameColumn(
                name: "fecha_fin",
                table: "puestos",
                newName: "fecha_baja");

            migrationBuilder.RenameColumn(
                name: "fecha_baja",
                table: "docentes",
                newName: "fecha_baja_institucion");

            migrationBuilder.RenameColumn(
                name: "fecha_alta",
                table: "docentes",
                newName: "fecha_alta_institucion");
        }
    }
}
