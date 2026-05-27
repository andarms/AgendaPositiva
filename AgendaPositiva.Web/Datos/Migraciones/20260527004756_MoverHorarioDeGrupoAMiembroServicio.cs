using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class MoverHorarioDeGrupoAMiembroServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HorarioServicioId",
                table: "MiembrosGrupoServicio",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""MiembrosGrupoServicio"" m
                SET ""HorarioServicioId"" = g.""HorarioServicioId""
                FROM ""GruposServicio"" g
                WHERE m.""GrupoServicioId"" = g.""Id""
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupoServicio_HorarioServicioId",
                table: "MiembrosGrupoServicio",
                column: "HorarioServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MiembrosGrupoServicio_HorariosServicio_HorarioServicioId",
                table: "MiembrosGrupoServicio",
                column: "HorarioServicioId",
                principalTable: "HorariosServicio",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropForeignKey(
                name: "FK_GruposServicio_HorariosServicio_HorarioServicioId",
                table: "GruposServicio");

            migrationBuilder.DropIndex(
                name: "IX_GruposServicio_HorarioServicioId",
                table: "GruposServicio");

            migrationBuilder.DropColumn(
                name: "HorarioServicioId",
                table: "GruposServicio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MiembrosGrupoServicio_HorariosServicio_HorarioServicioId",
                table: "MiembrosGrupoServicio");

            migrationBuilder.DropIndex(
                name: "IX_MiembrosGrupoServicio_HorarioServicioId",
                table: "MiembrosGrupoServicio");

            migrationBuilder.DropColumn(
                name: "HorarioServicioId",
                table: "MiembrosGrupoServicio");

            migrationBuilder.AddColumn<int>(
                name: "HorarioServicioId",
                table: "GruposServicio",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""GruposServicio"" g
                SET ""HorarioServicioId"" = COALESCE(
                    (
                        SELECT m.""HorarioServicioId""
                        FROM ""MiembrosGrupoServicio"" m
                        WHERE m.""GrupoServicioId"" = g.""Id""
                          AND m.""HorarioServicioId"" IS NOT NULL
                        ORDER BY m.""Id""
                        LIMIT 1
                    ),
                    (
                        SELECT h.""Id""
                        FROM ""HorariosServicio"" h
                        WHERE h.""ServicioId"" = g.""ServicioId""
                        ORDER BY h.""FechaHoraInicio""
                        LIMIT 1
                    )
                )
            ");

            migrationBuilder.AlterColumn<int>(
                name: "HorarioServicioId",
                table: "GruposServicio",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposServicio_HorarioServicioId",
                table: "GruposServicio",
                column: "HorarioServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_GruposServicio_HorariosServicio_HorarioServicioId",
                table: "GruposServicio",
                column: "HorarioServicioId",
                principalTable: "HorariosServicio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
