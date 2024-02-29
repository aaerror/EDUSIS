using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_DATABASE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel_educativo = table.Column<string>(type: "varchar(15)", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSOS", x => x.curso_id);
                });

            migrationBuilder.CreateTable(
                name: "personas",
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
                    table.PrimaryKey("PK_personas", x => x.persona_id);
                });

            migrationBuilder.CreateTable(
                name: "materias",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    horas_catedra = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATERIAS", x => x.materia_id);
                    table.ForeignKey(
                        name: "FK_CURSOS_MATERIAS",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alumnos",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true)
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
                    cuil = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true),
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
                    altura = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    vivienda = table.Column<string>(type: "varchar(20)", nullable: false),
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
                name: "horarios",
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
                    table.PrimaryKey("PK_HORARIOS", x => new { x.materia_id, x.horario_id });
                    table.ForeignKey(
                        name: "FK_MATERIAS_HORARIOS",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "divisiones",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripicion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    preceptor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIVISIONES", x => x.division_id);
                    table.ForeignKey(
                        name: "FK_CURSOS_DIVISIONES",
                        column: x => x.curso_id,
                        principalTable: "cursos",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOCENTES_DIVISIONES",
                        column: x => x.preceptor_id,
                        principalTable: "docentes",
                        principalColumn: "persona_id");
                });

            migrationBuilder.CreateTable(
                name: "licencias",
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
                    table.PrimaryKey("PK_LICENCIAS", x => new { x.licencia_id, x.docente_id });
                    table.ForeignKey(
                        name: "FK_DOCENTES_LICENCIAS",
                        column: x => x.docente_id,
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
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true)
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
                        name: "FK_DOCENTES_SITUACION-REVISTA",
                        column: x => x.profesor_id,
                        principalTable: "docentes",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MATERIAS_SITUACION-REVISTA",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cursantes",
                columns: table => new
                {
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alumno_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    periodo = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false)
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
                        name: "FK_DIVISIONES_CURSANTES",
                        column: x => x.division_id,
                        principalTable: "divisiones",
                        principalColumn: "division_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calificaciones",
                columns: table => new
                {
                    calificacion_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    instancia = table.Column<string>(type: "varchar(15)", nullable: false),
                    nota = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    asistencia = table.Column<bool>(type: "bit", nullable: false),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calificaciones", x => x.calificacion_id);
                    table.ForeignKey(
                        name: "FK_CURSANTES_CALIFICACIONES",
                        column: x => x.cursante_id,
                        principalTable: "cursantes",
                        principalColumn: "cursante_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MATERIAS_CALIFICACIONES",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "materia_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_calificaciones_cursante_id",
                table: "calificaciones",
                column: "cursante_id");

            migrationBuilder.CreateIndex(
                name: "IX_calificaciones_materia_id",
                table: "calificaciones",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursantes_alumno_id",
                table: "cursantes",
                column: "alumno_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursantes_division_id",
                table: "cursantes",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_divisiones_curso_id",
                table: "divisiones",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "IX_divisiones_preceptor_id",
                table: "divisiones",
                column: "preceptor_id",
                unique: true,
                filter: "[preceptor_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_licencias_docente_id",
                table: "licencias",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_materias_curso_id",
                table: "materias",
                column: "curso_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_profesor_id",
                table: "situacion_revista",
                column: "profesor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calificaciones");

            migrationBuilder.DropTable(
                name: "domicilios");

            migrationBuilder.DropTable(
                name: "horarios");

            migrationBuilder.DropTable(
                name: "licencias");

            migrationBuilder.DropTable(
                name: "puestos");

            migrationBuilder.DropTable(
                name: "situacion_revista");

            migrationBuilder.DropTable(
                name: "cursantes");

            migrationBuilder.DropTable(
                name: "materias");

            migrationBuilder.DropTable(
                name: "alumnos");

            migrationBuilder.DropTable(
                name: "divisiones");

            migrationBuilder.DropTable(
                name: "cursos");

            migrationBuilder.DropTable(
                name: "docentes");

            migrationBuilder.DropTable(
                name: "personas");
        }
    }
}
