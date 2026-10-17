using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ciclo_lectivo",
                columns: table => new
                {
                    periodo = table.Column<short>(type: "smallint", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CICLO_LECTIVO", x => x.periodo);
                });

            migrationBuilder.CreateTable(
                name: "curso",
                columns: table => new
                {
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    nivel_educativo = table.Column<string>(type: "varchar(15)", nullable: false),
                    grado = table.Column<string>(type: "varchar(10)", nullable: false)
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
                    apellido = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    documento = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    sexo = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "date", nullable: false),
                    nacionalidad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    telefono = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona", x => x.persona_id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL", x => x.rol_id);
                });

            migrationBuilder.CreateTable(
                name: "diseno_curricular",
                columns: table => new
                {
                    diseno_curricular_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISENO_CURRICULAR", x => x.diseno_curricular_id);
                    table.ForeignKey(
                        name: "FK_DISENO_CURRICULAR_CURSO",
                        column: x => x.curso_id,
                        principalTable: "curso",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alumno",
                columns: table => new
                {
                    persona_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    legajo = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true)
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
                    legajo = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    cuil = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false)
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
                    calle = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    altura = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: true),
                    vivienda = table.Column<string>(type: "varchar(20)", nullable: false),
                    observaciones = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    localidad = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    provincia = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    pais = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOMICILIO", x => x.persona_id);
                    table.ForeignKey(
                        name: "FK_DOMICILIO_PERSONA",
                        column: x => x.persona_id,
                        principalTable: "persona",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "materia",
                columns: table => new
                {
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    diseno_curricular_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    horas_catedra = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATERIA", x => x.materia_id);
                    table.ForeignKey(
                        name: "FK_MATERIA_DISENO_CURRICULAR",
                        column: x => x.diseno_curricular_id,
                        principalTable: "diseno_curricular",
                        principalColumn: "diseno_curricular_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "division",
                columns: table => new
                {
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    curso_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false),
                    preceptor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIVISION", x => x.division_id);
                    table.ForeignKey(
                        name: "FK_DIVISION_CURSO",
                        column: x => x.curso_id,
                        principalTable: "curso",
                        principalColumn: "curso_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DIVISION_DOCENTE",
                        column: x => x.preceptor_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "licencia",
                columns: table => new
                {
                    licencia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    articulo = table.Column<string>(type: "varchar(15)", nullable: false),
                    estado = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true),
                    observacion = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICENCIA", x => x.licencia_id);
                    table.ForeignKey(
                        name: "FK_LICENCIA_DOCENTE",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "puesto",
                columns: table => new
                {
                    puesto_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    posicion = table.Column<string>(type: "varchar(15)", nullable: false),
                    estado = table.Column<string>(type: "varchar(15)", nullable: false),
                    es_eventual = table.Column<bool>(type: "bit", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PUESTO", x => x.puesto_id);
                    table.ForeignKey(
                        name: "FK_PUESTO_DOCENTE",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    usuario = table.Column<string>(type: "varchar(18)", unicode: false, maxLength: 18, nullable: false),
                    password_salt = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_USUARIO_DOCENTE",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cursante",
                columns: table => new
                {
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alumno_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ciclo_lectivo = table.Column<short>(type: "smallint", maxLength: 50, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true),
                    es_recursante = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURSANTE", x => x.cursante_id);
                    table.ForeignKey(
                        name: "FK_CURSANTE_ALUMNO",
                        column: x => x.alumno_id,
                        principalTable: "alumno",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CURSANTE_CICLO_LECTIVO",
                        column: x => x.ciclo_lectivo,
                        principalTable: "ciclo_lectivo",
                        principalColumn: "periodo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CURSANTE_DIVISION",
                        column: x => x.division_id,
                        principalTable: "division",
                        principalColumn: "division_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "planilla_asistencia",
                columns: table => new
                {
                    planilla_asistencia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: false),
                    preceptor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cerrada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLANILLA_ASISTENCIA", x => x.planilla_asistencia_id);
                    table.ForeignKey(
                        name: "FK_PLANILLA_ASISTENCIA_DIVISION",
                        column: x => x.division_id,
                        principalTable: "division",
                        principalColumn: "division_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PLANILLA_ASISTENCIA_DOCENTE",
                        column: x => x.preceptor_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_ROL", x => new { x.rol_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_ROL",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_USUARIO",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calificacion",
                columns: table => new
                {
                    calificacion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: false),
                    instancia = table.Column<string>(type: "varchar(20)", nullable: false),
                    rindio = table.Column<bool>(type: "bit", nullable: false),
                    nota = table.Column<double>(type: "float(24)", nullable: true),
                    observacion = table.Column<string>(type: "varchar(140)", unicode: false, maxLength: 140, nullable: true),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALIFICACION", x => x.calificacion_id);
                    table.ForeignKey(
                        name: "FK_CALIFICACION_CURSANTE",
                        column: x => x.cursante_id,
                        principalTable: "cursante",
                        principalColumn: "cursante_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CALIFICACION_MATERIA",
                        column: x => x.materia_id,
                        principalTable: "materia",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registro_asistencia",
                columns: table => new
                {
                    registro_asistencia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cursante_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tipo = table.Column<string>(type: "varchar(15)", nullable: false),
                    minutos = table.Column<TimeSpan>(type: "time", nullable: true),
                    observacion = table.Column<string>(type: "varchar(140)", unicode: false, maxLength: 140, nullable: true),
                    planilla_asistencia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGISTRO_ASISTENCIA", x => x.registro_asistencia_id);
                    table.ForeignKey(
                        name: "FK_REGISTRO_ASISTENCIA_CURSANTE",
                        column: x => x.cursante_id,
                        principalTable: "cursante",
                        principalColumn: "cursante_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA",
                        column: x => x.planilla_asistencia_id,
                        principalTable: "planilla_asistencia",
                        principalColumn: "planilla_asistencia_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catedra",
                columns: table => new
                {
                    catedra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    materia_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    division_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carga_horaria = table.Column<int>(type: "int", nullable: false),
                    situacion_en_funciones_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEDRA", x => x.catedra_id);
                    table.ForeignKey(
                        name: "FK_CATEDRA_DIVISION",
                        column: x => x.division_id,
                        principalTable: "division",
                        principalColumn: "division_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CATEDRA_MATERIA",
                        column: x => x.materia_id,
                        principalTable: "materia",
                        principalColumn: "materia_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horario",
                columns: table => new
                {
                    catedra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    horario_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    turno = table.Column<string>(type: "varchar(10)", nullable: false),
                    dia = table.Column<string>(type: "varchar(10)", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    hora_fin = table.Column<TimeSpan>(type: "time(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HORARIO", x => new { x.catedra_id, x.horario_id });
                    table.ForeignKey(
                        name: "FK_HORARIO_CATEDRA",
                        column: x => x.catedra_id,
                        principalTable: "catedra",
                        principalColumn: "catedra_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "situacion_revista",
                columns: table => new
                {
                    situacion_revista_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    estado = table.Column<string>(type: "varchar(20)", nullable: false),
                    cargo = table.Column<string>(type: "varchar(10)", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true),
                    reemplaza_a = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    catedra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SITUACION_REVISTA", x => x.situacion_revista_id);
                    table.UniqueConstraint("AK_SITUACION_REVISTA_CATEDRA", x => new { x.catedra_id, x.situacion_revista_id });
                    table.ForeignKey(
                        name: "FK_SITUACION_REVISTA_CATEDRA",
                        column: x => x.catedra_id,
                        principalTable: "catedra",
                        principalColumn: "catedra_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SITUACION_REVISTA_DOCENTE",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SITUACION_REVISTA_REEMPLAZA_A",
                        column: x => x.reemplaza_a,
                        principalTable: "situacion_revista",
                        principalColumn: "situacion_revista_id");
                });

            migrationBuilder.InsertData(
                table: "ciclo_lectivo",
                column: "periodo",
                values: new object[]
                {
                    (short)2020,
                    (short)2021,
                    (short)2022,
                    (short)2023,
                    (short)2024,
                    (short)2025,
                    (short)2026,
                    (short)2027,
                    (short)2028,
                    (short)2029,
                    (short)2030,
                    (short)2031,
                    (short)2032,
                    (short)2033,
                    (short)2034,
                    (short)2035
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "rol_id", "descripcion" },
                values: new object[,]
                {
                    { 1, "Administrador" },
                    { 2, "Dirección" },
                    { 3, "Docente" },
                    { 4, "Secretaria" }
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
                name: "IX_catedra_catedra_id_situacion_en_funciones_id",
                table: "catedra",
                columns: new[] { "catedra_id", "situacion_en_funciones_id" },
                unique: true,
                filter: "[situacion_en_funciones_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_catedra_division_id",
                table: "catedra",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_catedra_materia_id_division_id",
                table: "catedra",
                columns: new[] { "materia_id", "division_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cursante_alumno_id",
                table: "cursante",
                column: "alumno_id");

            migrationBuilder.CreateIndex(
                name: "IX_cursante_ciclo_lectivo",
                table: "cursante",
                column: "ciclo_lectivo");

            migrationBuilder.CreateIndex(
                name: "IX_cursante_division_id",
                table: "cursante",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_diseno_curricular_curso_id",
                table: "diseno_curricular",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "IX_division_curso_id",
                table: "division",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "IX_division_preceptor_id",
                table: "division",
                column: "preceptor_id");

            migrationBuilder.CreateIndex(
                name: "IX_licencia_docente_id",
                table: "licencia",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_materia_diseno_curricular_id_descripcion",
                table: "materia",
                columns: new[] { "diseno_curricular_id", "descripcion" },
                unique: true);

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
                name: "IX_planilla_asistencia_division_id_fecha",
                table: "planilla_asistencia",
                columns: new[] { "division_id", "fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_planilla_asistencia_preceptor_id",
                table: "planilla_asistencia",
                column: "preceptor_id");

            migrationBuilder.CreateIndex(
                name: "IX_puesto_docente_id",
                table: "puesto",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_registro_asistencia_cursante_id",
                table: "registro_asistencia",
                column: "cursante_id");

            migrationBuilder.CreateIndex(
                name: "IX_registro_asistencia_planilla_asistencia_id_cursante_id",
                table: "registro_asistencia",
                columns: new[] { "planilla_asistencia_id", "cursante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_docente_id",
                table: "situacion_revista",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "IX_situacion_revista_reemplaza_a",
                table: "situacion_revista",
                column: "reemplaza_a");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_docente_id",
                table: "usuario",
                column: "docente_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_usuario_id",
                table: "usuario_rol",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CATEDRA_SITUACION_REVISTA",
                table: "catedra",
                columns: new[] { "catedra_id", "situacion_en_funciones_id" },
                principalTable: "situacion_revista",
                principalColumns: new[] { "catedra_id", "situacion_revista_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_docente_persona_persona_id",
                table: "docente");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEDRA_MATERIA",
                table: "catedra");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEDRA_DIVISION",
                table: "catedra");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEDRA_SITUACION_REVISTA",
                table: "catedra");

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
                name: "registro_asistencia");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "cursante");

            migrationBuilder.DropTable(
                name: "planilla_asistencia");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "alumno");

            migrationBuilder.DropTable(
                name: "ciclo_lectivo");

            migrationBuilder.DropTable(
                name: "persona");

            migrationBuilder.DropTable(
                name: "materia");

            migrationBuilder.DropTable(
                name: "diseno_curricular");

            migrationBuilder.DropTable(
                name: "division");

            migrationBuilder.DropTable(
                name: "curso");

            migrationBuilder.DropTable(
                name: "situacion_revista");

            migrationBuilder.DropTable(
                name: "catedra");

            migrationBuilder.DropTable(
                name: "docente");
        }
    }
}
