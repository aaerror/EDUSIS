using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables_DOCENTES_PROFESORES_PRECEPTORES : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "docentes",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CUIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fecha_alta_institucion = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja_institucion = table.Column<DateTime>(type: "date", nullable: false),
                    esta_activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_docentes", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_docentes_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "preceptores",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "puestos",
                columns: table => new
                {
                    puesto_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    posicion = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUESTOS", x => new { x.puesto_id, x.docente_id });
                    table.ForeignKey(
                        name: "FK_DOCENTES_PUESTOS",
                        column: x => x.docente_id,
                        principalTable: "docentes",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_puestos_docente_id",
                table: "puestos",
                column: "docente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "preceptores");

            migrationBuilder.DropTable(
                name: "profesores");

            migrationBuilder.DropTable(
                name: "puestos");

            migrationBuilder.DropTable(
                name: "docentes");
        }
    }
}
