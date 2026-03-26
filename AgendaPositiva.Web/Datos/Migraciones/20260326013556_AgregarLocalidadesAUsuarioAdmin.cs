using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarLocalidadesAUsuarioAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Transformar datos: convertir JSON array ["Depto1","Depto2"] a objeto {"Depto1":[],"Depto2":[]}
            migrationBuilder.Sql("""
                UPDATE "UsuariosAdministradores"
                SET "Departamentos" = (
                    SELECT jsonb_object_agg(elem, '[]'::jsonb)
                    FROM jsonb_array_elements_text("Departamentos") AS elem
                )
                WHERE jsonb_typeof("Departamentos") = 'array'
                  AND jsonb_array_length("Departamentos") > 0;

                UPDATE "UsuariosAdministradores"
                SET "Departamentos" = '{}'::jsonb
                WHERE jsonb_typeof("Departamentos") = 'array'
                  AND jsonb_array_length("Departamentos") = 0;
                """);

            migrationBuilder.RenameColumn(
                name: "Departamentos",
                table: "UsuariosAdministradores",
                newName: "Localidades");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Localidades",
                table: "UsuariosAdministradores",
                newName: "Departamentos");

            // Revertir: convertir JSON objeto {"Depto1":[],"Depto2":[]} a array ["Depto1","Depto2"]
            migrationBuilder.Sql("""
                UPDATE "UsuariosAdministradores"
                SET "Departamentos" = (
                    SELECT jsonb_agg(key)
                    FROM jsonb_each("Departamentos") AS t(key, value)
                )
                WHERE jsonb_typeof("Departamentos") = 'object';
                """);
        }
    }
}
