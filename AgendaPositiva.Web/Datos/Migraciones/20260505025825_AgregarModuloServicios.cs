using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarModuloServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CantidadPersonasRequeridas = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HorariosServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosServicio_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    HorarioServicioId = table.Column<int>(type: "integer", nullable: false),
                    LiderInscripcionId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposServicio_HorariosServicio_HorarioServicioId",
                        column: x => x.HorarioServicioId,
                        principalTable: "HorariosServicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GruposServicio_Inscripciones_LiderInscripcionId",
                        column: x => x.LiderInscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GruposServicio_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MiembrosGrupoServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupoServicioId = table.Column<int>(type: "integer", nullable: false),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiembrosGrupoServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiembrosGrupoServicio_GruposServicio_GrupoServicioId",
                        column: x => x.GrupoServicioId,
                        principalTable: "GruposServicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MiembrosGrupoServicio_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GruposServicio_HorarioServicioId",
                table: "GruposServicio",
                column: "HorarioServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposServicio_LiderInscripcionId",
                table: "GruposServicio",
                column: "LiderInscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposServicio_ServicioId",
                table: "GruposServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosServicio_ServicioId",
                table: "HorariosServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupoServicio_GrupoServicioId_InscripcionId",
                table: "MiembrosGrupoServicio",
                columns: new[] { "GrupoServicioId", "InscripcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupoServicio_InscripcionId",
                table: "MiembrosGrupoServicio",
                column: "InscripcionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MiembrosGrupoServicio");

            migrationBuilder.DropTable(
                name: "GruposServicio");

            migrationBuilder.DropTable(
                name: "HorariosServicio");

            migrationBuilder.DropTable(
                name: "Servicios");
        }
    }
}
