using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPreguntasAlimentacionYComunion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionAlergia",
                table: "Inscripciones",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParticipaComunionAncianos",
                table: "Inscripciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereAlimentacion",
                table: "Inscripciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneAlergiaAlimentaria",
                table: "Inscripciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescripcionAlergia",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "ParticipaComunionAncianos",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "RequiereAlimentacion",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "TieneAlergiaAlimentaria",
                table: "Inscripciones");
        }
    }
}
