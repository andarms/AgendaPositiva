using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RenombrarUsuarioSistemaAAdministrador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UsuariosSistema",
                newName: "UsuariosAdministradores");

            migrationBuilder.RenameIndex(
                name: "PK_UsuariosSistema",
                table: "UsuariosAdministradores",
                newName: "PK_UsuariosAdministradores");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAdministradores_Email",
                table: "UsuariosAdministradores",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsuariosAdministradores_Email",
                table: "UsuariosAdministradores");

            migrationBuilder.RenameTable(
                name: "UsuariosAdministradores",
                newName: "UsuariosSistema");

            migrationBuilder.RenameIndex(
                name: "PK_UsuariosAdministradores",
                table: "UsuariosSistema",
                newName: "PK_UsuariosSistema");
        }
    }
}
