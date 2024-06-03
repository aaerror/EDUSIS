using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_USUARIO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    docente_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rol = table.Column<string>(type: "varchar(15)", nullable: false),
                    usuario = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    password_salt = table.Column<byte[]>(type: "varbinary(256)", nullable: false),
                    password_hash = table.Column<byte[]>(type: "varbinary(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_DOCENTE_USUARIO",
                        column: x => x.docente_id,
                        principalTable: "docente",
                        principalColumn: "persona_id");
                });

            migrationBuilder.CreateTable(
                name: "acceso",
                columns: table => new
                {
                    acceso_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permiso = table.Column<string>(type: "varchar(15)", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCESO", x => new { x.acceso_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_USUARIO_PERMISO",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acceso_usuario_id",
                table: "acceso",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_docente_id",
                table: "usuario",
                column: "docente_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acceso");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
