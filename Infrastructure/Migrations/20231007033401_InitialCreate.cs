using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apellido = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    documento = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    sexo = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "datetime", nullable: false),
                    nacionalidad = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    telefono = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personas", x => x.persona_id);
                });

            migrationBuilder.CreateTable(
                name: "alumnos",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alumnos", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_alumnos_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "domicilios",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calle = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    altura = table.Column<int>(type: "int", nullable: false),
                    vivienda = table.Column<string>(type: "varchar(10)", nullable: false),
                    observaciones = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    localidad = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    provincia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    pais = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOMICILIOS", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_PERSONAS_DOMICILIOS",
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
                name: "IX_personas_documento",
                table: "personas",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personas_email",
                table: "personas",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puestos_docente_id",
                table: "puestos",
                column: "docente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alumnos");

            migrationBuilder.DropTable(
                name: "domicilios");

            migrationBuilder.DropTable(
                name: "preceptores");

            migrationBuilder.DropTable(
                name: "profesores");

            migrationBuilder.DropTable(
                name: "puestos");

            migrationBuilder.DropTable(
                name: "docentes");

            migrationBuilder.DropTable(
                name: "personas");
        }
    }
}
