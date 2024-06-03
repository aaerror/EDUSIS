using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "curso",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel_educativo = table.Column<string>(type: "varchar(15)", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSO", x => x.curso_id);
                });

            migrationBuilder.CreateTable(
                name: "persona",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apellido = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    documento = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    sexo = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "date", nullable: false),
                    nacionalidad = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    telefono = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona", x => x.persona_id);
                });

            migrationBuilder.CreateTable(
                name: "materia",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    horas_catedra = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATERIA", x => x.materia_id);
                    table.ForeignKey(
                        name: "FK_CURSO_MATERIA",
                        column: x => x.curso_id,
                        principalTable: "curso",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alumno",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alumno", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_alumno_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "docente",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    cuil = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true),
                    esta_activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_docente", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_docente_persona_persona_id",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "domicilio",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calle = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    altura = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    vivienda = table.Column<string>(type: "varchar(20)", nullable: false),
                    observaciones = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    localidad = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    provincia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    pais = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOMICILIO", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_PERSONA_DOMICILIO",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "horario",
                columns: table => new
                {
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
                    table.PrimaryKey("PK_HORARIO", x => new { x.materia_id, x.horario_id });
                    table.ForeignKey(
                        name: "FK_MATERIA_HORARIO",
                        column: x => x.materia_id,
                        principalTable: "materia",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "division",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripicion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    preceptor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIVISION", x => x.division_id);
                    table.ForeignKey(
                        name: "FK_CURSO_DIVISION",
                        column: x => x.curso_id,
                        principalTable: "curso",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOCENTE_DIVISION",
                        column: x => x.preceptor_id,
                        principalTable: "docente",
                        principalColumn: "persona_id");
                });

            migrationBuilder.CreateTable(
                name: "licencia",
                columns: table => new
                {
                    licencia_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    articulo = table.Column<string>(type: "varchar(15)", nullable: false),
                    estado = table.Column<string>(type: "varchar(15)", nullable: false),
                    dias = table.Column<int>(type: "int", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    observacion = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICENCIA", x => new { x.licencia_id, x.docente_id });
                    table.ForeignKey(
                        name: "FK_DOCENTE_LICENCIA",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "puesto",
                columns: table => new
                {
                    puesto_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    posicion = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUESTO", x => new { x.puesto_id, x.docente_id });
                    table.ForeignKey(
                        name: "FK_DOCENTE_PUESTO",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "situacion_revista",
                columns: table => new
                {
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    situacion_revista_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    profesor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cargo = table.Column<string>(type: "varchar(10)", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true),
                    en_funciones = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SITUACION-REVISTA", x => new { x.materia_id, x.situacion_revista_id });
                    table.ForeignKey(
                        name: "FK_DOCENTE_SITUACION-REVISTA",
                        column: x => x.profesor_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MATERIA_SITUACION-REVISTA",
                        column: x => x.materia_id,
                        principalTable: "materia",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cursante",
                columns: table => new
                {
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alumno_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    periodo = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSANTE", x => x.cursante_id);
                    table.ForeignKey(
                        name: "FK_ALUMNO_CURSANTE",
                        column: x => x.alumno_id,
                        principalTable: "alumno",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DIVISION_CURSANTE",
                        column: x => x.division_id,
                        principalTable: "division",
                        principalColumn: "division_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calificacion",
                columns: table => new
                {
                    calificacion_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: true),
                    instancia = table.Column<string>(type: "varchar(15)", nullable: false),
                    nota = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    asistencia = table.Column<bool>(type: "bit", nullable: false),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calificacion", x => x.calificacion_id);
                    table.ForeignKey(
                        name: "FK_CURSANTE_CALIFICACION",
                        column: x => x.cursante_id,
                        principalTable: "cursante",
                        principalColumn: "cursante_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MATERIA_CALIFICACION",
                        column: x => x.materia_id,
                        principalTable: "materia",
                        principalColumn: "materia_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_calificacion_cursante_id",
                table: "calificacion",
                column: "cursante_id");

            migrationBuilder.CreateIndex(
                name: "IX_calificacion_materia_id",
                table: "calificacion",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursante_alumno_id",
                table: "cursante",
                column: "alumno_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursante_division_id",
                table: "cursante",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_division_curso_id",
                table: "division",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "IX_division_preceptor_id",
                table: "division",
                column: "preceptor_id",
                unique: true,
                filter: "[preceptor_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_licencia_docente_id",
                table: "licencia",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_materia_curso_id",
                table: "materia",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "IX_persona_documento",
                table: "persona",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_email",
                table: "persona",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puesto_docente_id",
                table: "puesto",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista",
                column: "profesor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calificacion");

            migrationBuilder.DropTable(
                name: "domicilio");

            migrationBuilder.DropTable(
                name: "horario");

            migrationBuilder.DropTable(
                name: "licencia");

            migrationBuilder.DropTable(
                name: "puesto");

            migrationBuilder.DropTable(
                name: "situacion_revista");

            migrationBuilder.DropTable(
                name: "cursante");

            migrationBuilder.DropTable(
                name: "materia");

            migrationBuilder.DropTable(
                name: "alumno");

            migrationBuilder.DropTable(
                name: "division");

            migrationBuilder.DropTable(
                name: "curso");

            migrationBuilder.DropTable(
                name: "docente");

            migrationBuilder.DropTable(
                name: "persona");
        }
    }
}
