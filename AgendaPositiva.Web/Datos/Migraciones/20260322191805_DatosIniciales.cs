using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class DatosIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrupoFamiliar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LiderGrupoId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupoFamiliar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombres = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TipoIdentificacion = table.Column<int>(type: "integer", nullable: false),
                    NumeroIdentificacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    RequiereHospedaje = table.Column<bool>(type: "boolean", nullable: false),
                    GrupoAsistenciaId = table.Column<int>(type: "integer", nullable: true),
                    RelacionConLider = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    NecesidadesEspeciales = table.Column<string>(type: "text", nullable: true),
                    Localidad = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_GrupoFamiliar_GrupoAsistenciaId",
                        column: x => x.GrupoAsistenciaId,
                        principalTable: "GrupoFamiliar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Slug",
                table: "Eventos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_EventoId",
                table: "Inscripciones",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_GrupoAsistenciaId",
                table: "Inscripciones",
                column: "GrupoAsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_PersonaId",
                table: "Inscripciones",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_TipoIdentificacion_NumeroIdentificacion",
                table: "Personas",
                columns: new[] { "TipoIdentificacion", "NumeroIdentificacion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropTable(
                name: "UsuariosSistema");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "GrupoFamiliar");

            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}
