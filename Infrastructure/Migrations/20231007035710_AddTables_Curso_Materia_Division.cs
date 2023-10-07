using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables_Curso_Materia_Division : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ciclo_lectivo",
                columns: table => new
                {
                    ciclo_lectivo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    periodo = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CICLO-LECTIVO", x => x.ciclo_lectivo_id);
                });

            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    formacion = table.Column<string>(type: "varchar(15)", nullable: false),
                    descripcion = table.Column<string>(type: "char(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSOS", x => x.curso_id);
                });

            migrationBuilder.CreateTable(
                name: "divisiones",
                columns: table => new
                {
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripicion = table.Column<string>(type: "char(1)", nullable: false),
                    preceptor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIVISIONES", x => new { x.curso_id, x.division_id });
                    table.ForeignKey(
                        name: "FK_CURSOS_DIVISIONES",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PRECEPTORES_DIVISIONES",
                        column: x => x.preceptor_id,
                        principalTable: "preceptores",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "materias",
                columns: table => new
                {
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    horas_catedra = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATERIAS", x => new { x.curso_id, x.materia_id });
                    table.ForeignKey(
                        name: "FK_CURSOS_MATERIAS",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cursantes",
                columns: table => new
                {
                    cursante_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    alumno_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ciclo_lectivo_id = table.Column<int>(type: "int", nullable: false),
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSANTES", x => x.cursante_id);
                    table.ForeignKey(
                        name: "FK_ALUMNOS_CURSANTES",
                        column: x => x.alumno_id,
                        principalTable: "alumnos",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CICLO-LECTIVO_CURSANTES",
                        column: x => x.ciclo_lectivo_id,
                        principalTable: "ciclo_lectivo",
                        principalColumn: "ciclo_lectivo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DIVISIONES_CURSANTES",
                        columns: x => new { x.curso_id, x.division_id },
                        principalTable: "divisiones",
                        principalColumns: new[] { "curso_id", "division_id" });
                });

            migrationBuilder.CreateTable(
                name: "horarios",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    horario_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    turno = table.Column<string>(type: "varchar(10)", nullable: false),
                    dia = table.Column<string>(type: "varchar(10)", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    hora_fin = table.Column<TimeSpan>(type: "time(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HORARIOS", x => new { x.curso_id, x.materia_id, x.horario_id });
                    table.ForeignKey(
                        name: "FK_MATERIAS_HORARIOS",
                        columns: x => new { x.curso_id, x.materia_id },
                        principalTable: "materias",
                        principalColumns: new[] { "curso_id", "materia_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "situacion_revista",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    situacion_revista_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    profesor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cargo = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SITUACION-REVISTA", x => new { x.curso_id, x.materia_id, x.situacion_revista_id });
                    table.ForeignKey(
                        name: "FK_MATERIAS_SITUACION-REVISTA",
                        columns: x => new { x.curso_id, x.materia_id },
                        principalTable: "materias",
                        principalColumns: new[] { "curso_id", "materia_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROFESORES_SITUACION-REVISTA",
                        column: x => x.profesor_id,
                        principalTable: "profesores",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cursantes_alumno_id",
                table: "cursantes",
                column: "alumno_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cursantes_ciclo_lectivo_id",
                table: "cursantes",
                column: "ciclo_lectivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursantes_curso_id_division_id",
                table: "cursantes",
                columns: new[] { "curso_id", "division_id" });

            migrationBuilder.CreateIndex(
                name: "IX_divisiones_preceptor_id",
                table: "divisiones",
                column: "preceptor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista",
                column: "profesor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cursantes");

            migrationBuilder.DropTable(
                name: "horarios");

            migrationBuilder.DropTable(
                name: "situacion_revista");

            migrationBuilder.DropTable(
                name: "ciclo_lectivo");

            migrationBuilder.DropTable(
                name: "divisiones");

            migrationBuilder.DropTable(
                name: "materias");

            migrationBuilder.DropTable(
                name: "cursos");
        }
    }
}
