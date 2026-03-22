using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AddGenero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Genero",
                table: "Personas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Personas");
        }
    }
}
