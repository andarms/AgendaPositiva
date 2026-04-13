using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarUsuarioRegiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuarioRegiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioAdministradorId = table.Column<int>(type: "integer", nullable: false),
                    RegionEventoId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRegiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioRegiones_RegionesEvento_RegionEventoId",
                        column: x => x.RegionEventoId,
                        principalTable: "RegionesEvento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRegiones_UsuariosAdministradores_UsuarioAdministrado~",
                        column: x => x.UsuarioAdministradorId,
                        principalTable: "UsuariosAdministradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRegiones_RegionEventoId",
                table: "UsuarioRegiones",
                column: "RegionEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRegiones_UsuarioAdministradorId_RegionEventoId",
                table: "UsuarioRegiones",
                columns: new[] { "UsuarioAdministradorId", "RegionEventoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioRegiones");
        }
    }
}
