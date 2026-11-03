using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolesPreceptoriaYSecretariaAcademica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "descripcion",
                table: "rol",
                type: "varchar(25)",
                unicode: false,
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldUnicode: false,
                oldMaxLength: 15);

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "rol_id", "descripcion" },
                values: new object[,]
                {
                    { 5, "Preceptoría" },
                    { 6, "Secretaría Académica" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rol",
                keyColumn: "rol_id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "rol",
                keyColumn: "rol_id",
                keyValue: 6);

            migrationBuilder.AlterColumn<string>(
                name: "descripcion",
                table: "rol",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(25)",
                oldUnicode: false,
                oldMaxLength: 25);
        }
    }
}
