using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVersionDeConcurrenciaALasRaices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "usuario",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "planilla_asistencia",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "persona",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "materia",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "licencia",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "division",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "diseno_curricular",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "curso",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "cursante",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "catedra",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "version",
                table: "planilla_asistencia");

            migrationBuilder.DropColumn(
                name: "version",
                table: "persona");

            migrationBuilder.DropColumn(
                name: "version",
                table: "materia");

            migrationBuilder.DropColumn(
                name: "version",
                table: "licencia");

            migrationBuilder.DropColumn(
                name: "version",
                table: "division");

            migrationBuilder.DropColumn(
                name: "version",
                table: "diseno_curricular");

            migrationBuilder.DropColumn(
                name: "version",
                table: "curso");

            migrationBuilder.DropColumn(
                name: "version",
                table: "cursante");

            migrationBuilder.DropColumn(
                name: "version",
                table: "catedra");
        }
    }
}
