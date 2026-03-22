using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AddLiderGrupoNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GrupoFamiliar_LiderGrupoId",
                table: "GrupoFamiliar",
                column: "LiderGrupoId");

            migrationBuilder.AddForeignKey(
                name: "FK_GrupoFamiliar_Personas_LiderGrupoId",
                table: "GrupoFamiliar",
                column: "LiderGrupoId",
                principalTable: "Personas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrupoFamiliar_Personas_LiderGrupoId",
                table: "GrupoFamiliar");

            migrationBuilder.DropIndex(
                name: "IX_GrupoFamiliar_LiderGrupoId",
                table: "GrupoFamiliar");
        }
    }
}
