using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaPositiva.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarModuloHospedajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Casas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreResponsable = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TelefonoResponsable = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CuposSolteros = table.Column<int>(type: "integer", nullable: false),
                    CuposSolteras = table.Column<int>(type: "integer", nullable: false),
                    CuposParejas = table.Column<int>(type: "integer", nullable: false),
                    ResponsablePersonaId = table.Column<int>(type: "integer", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Casas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Casas_Personas_ResponsablePersonaId",
                        column: x => x.ResponsablePersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Hoteles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hoteles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HabitacionesHotel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HotelId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CamasSencillas = table.Column<int>(type: "integer", nullable: false),
                    CamasDobles = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitacionesHotel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitacionesHotel_Hoteles_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hoteles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesHospedaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    CasaId = table.Column<int>(type: "integer", nullable: true),
                    HabitacionHotelId = table.Column<int>(type: "integer", nullable: true),
                    TipoCupoCasa = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesHospedaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesHospedaje_Casas_CasaId",
                        column: x => x.CasaId,
                        principalTable: "Casas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AsignacionesHospedaje_HabitacionesHotel_HabitacionHotelId",
                        column: x => x.HabitacionHotelId,
                        principalTable: "HabitacionesHotel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AsignacionesHospedaje_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHospedaje_CasaId",
                table: "AsignacionesHospedaje",
                column: "CasaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHospedaje_HabitacionHotelId",
                table: "AsignacionesHospedaje",
                column: "HabitacionHotelId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesHospedaje_InscripcionId",
                table: "AsignacionesHospedaje",
                column: "InscripcionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Casas_ResponsablePersonaId",
                table: "Casas",
                column: "ResponsablePersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitacionesHotel_HotelId",
                table: "HabitacionesHotel",
                column: "HotelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesHospedaje");

            migrationBuilder.DropTable(
                name: "Casas");

            migrationBuilder.DropTable(
                name: "HabitacionesHotel");

            migrationBuilder.DropTable(
                name: "Hoteles");
        }
    }
}
