using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCategoriasYAbonos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaInscripcionId",
                table: "Inscripciones",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbonosInscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RegistradoPorUsuario = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonosInscripcion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbonosInscripcion_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasInscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasInscripcion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_CategoriaInscripcionId",
                table: "Inscripciones",
                column: "CategoriaInscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonosInscripcion_InscripcionId",
                table: "AbonosInscripcion",
                column: "InscripcionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_CategoriasInscripcion_CategoriaInscripcionId",
                table: "Inscripciones",
                column: "CategoriaInscripcionId",
                principalTable: "CategoriasInscripcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_CategoriasInscripcion_CategoriaInscripcionId",
                table: "Inscripciones");

            migrationBuilder.DropTable(
                name: "AbonosInscripcion");

            migrationBuilder.DropTable(
                name: "CategoriasInscripcion");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_CategoriaInscripcionId",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "CategoriaInscripcionId",
                table: "Inscripciones");
        }
    }
}
