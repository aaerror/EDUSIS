using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// El estado <c>Cancelada</c> de <c>Licencia</c> pasó a llamarse <c>Rechazada</c>: es una
    /// solicitud que no se concedió, no una licencia concedida que se canceló. El estado se guarda
    /// como texto y el mapeo lo lee con <c>Enum.Parse</c>, así que una fila con el nombre viejo
    /// rompería la lectura: sólo se migran los datos, el esquema no cambia.
    /// </summary>
    public partial class RenombrarLicenciaCanceladaARechazada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE licencia SET estado = 'Rechazada' WHERE estado = 'Cancelada';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE licencia SET estado = 'Cancelada' WHERE estado = 'Rechazada';");
        }
    }
}
