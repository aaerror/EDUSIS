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
                name: "Personas",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apellido = table.Column<string>(type: "varchar(50)", nullable: false),
                    nombre = table.Column<string>(type: "varchar(50)", nullable: false),
                    documento = table.Column<int>(type: "int", nullable: false),
                    sexo = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "datetime", nullable: false),
                    nacionalidad = table.Column<string>(type: "varchar(20)", nullable: false),
                    telefono = table.Column<string>(type: "varchar(15)", nullable: false),
                    email = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.persona_id);
                });

            migrationBuilder.CreateTable(
                name: "Alumnos",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alumnos", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_Alumnos_Personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "Personas",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Domicilios",
                columns: table => new
                {
                    PersonaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calle = table.Column<string>(type: "varchar(50)", nullable: false),
                    altura = table.Column<int>(type: "int", nullable: false),
                    vivienda = table.Column<string>(type: "varchar(10)", nullable: false),
                    observaciones = table.Column<string>(type: "varchar(120)", nullable: false),
                    localidad = table.Column<string>(type: "varchar(50)", nullable: false),
                    provincia = table.Column<string>(type: "varchar(50)", nullable: false),
                    pais = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domicilios", x => x.PersonaId);
                    table.ForeignKey(
                        name: "FK_Domicilios_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alumnos");

            migrationBuilder.DropTable(
                name: "Domicilios");

            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}
