using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarUbicacionesServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UbicacionServicioId",
                table: "MiembrosGrupoServicio",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UbicacionesServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionesServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UbicacionesServicio_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupoServicio_UbicacionServicioId",
                table: "MiembrosGrupoServicio",
                column: "UbicacionServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesServicio_ServicioId",
                table: "UbicacionesServicio",
                column: "ServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MiembrosGrupoServicio_UbicacionesServicio_UbicacionServicio~",
                table: "MiembrosGrupoServicio",
                column: "UbicacionServicioId",
                principalTable: "UbicacionesServicio",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MiembrosGrupoServicio_UbicacionesServicio_UbicacionServicio~",
                table: "MiembrosGrupoServicio");

            migrationBuilder.DropTable(
                name: "UbicacionesServicio");

            migrationBuilder.DropIndex(
                name: "IX_MiembrosGrupoServicio_UbicacionServicioId",
                table: "MiembrosGrupoServicio");

            migrationBuilder.DropColumn(
                name: "UbicacionServicioId",
                table: "MiembrosGrupoServicio");
        }
    }
}
