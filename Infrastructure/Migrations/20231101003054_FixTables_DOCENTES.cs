using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTables_DOCENTES : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PRECEPTORES_DIVISIONES",
                table: "divisiones");

            migrationBuilder.DropForeignKey(
                name: "FK_PROFESORES_SITUACION-REVISTA",
                table: "situacion_revista");

            migrationBuilder.DropTable(
                name: "preceptores");

            migrationBuilder.DropTable(
                name: "profesores");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCENTES_DIVISIONES",
                table: "divisiones",
                column: "preceptor_id",
                principalTable: "docentes",
                principalColumn: "persona_id");

            migrationBuilder.AddForeignKey(
                name: "FK_DOCENTES_SITUACION-REVISTA",
                table: "situacion_revista",
                column: "profesor_id",
                principalTable: "docentes",
                principalColumn: "persona_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DOCENTES_DIVISIONES",
                table: "divisiones");

            migrationBuilder.DropForeignKey(
                name: "FK_DOCENTES_SITUACION-REVISTA",
                table: "situacion_revista");

            migrationBuilder.CreateTable(
                name: "preceptores",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preceptores", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_preceptores_docentes_persona_id",
                        column: x => x.persona_id,
                        principalTable: "docentes",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profesores",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profesores", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_profesores_docentes_persona_id",
                        column: x => x.persona_id,
                        principalTable: "docentes",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PRECEPTORES_DIVISIONES",
                table: "divisiones",
                column: "preceptor_id",
                principalTable: "preceptores",
                principalColumn: "persona_id");

            migrationBuilder.AddForeignKey(
                name: "FK_PROFESORES_SITUACION-REVISTA",
                table: "situacion_revista",
                column: "profesor_id",
                principalTable: "profesores",
                principalColumn: "persona_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
