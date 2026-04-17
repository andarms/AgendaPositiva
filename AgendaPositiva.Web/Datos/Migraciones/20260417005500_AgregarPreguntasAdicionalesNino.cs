using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPreguntasAdicionalesNino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreguntasAdicionalesNino",
                table: "Inscripciones",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreguntasAdicionalesNino",
                table: "Inscripciones");
        }
    }
}
