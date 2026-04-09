
using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Inscripciones.Persistencia;

public static class InscripcionesDbContextExtensions
{
    public static void ConfigurarPersonas(this ModelBuilder builder)
    {
        builder.Entity<Persona>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();

            e.Property(p => p.Nombres).HasMaxLength(255).IsRequired();

            e.Property(p => p.Apellidos).HasMaxLength(255).IsRequired();

            e.Property(p => p.FechaNacimiento).IsRequired();

            e.Property(p => p.Telefono).HasMaxLength(50).IsRequired();

            e.Property(p => p.Email).HasMaxLength(255);

            e.Property(p => p.TipoIdentificacion).IsRequired();

            e.Property(p => p.NumeroIdentificacion).HasMaxLength(100).IsRequired();

            e.HasIndex(p => new { p.TipoIdentificacion, p.NumeroIdentificacion }).IsUnique();

            e.Ignore(p => p.NombreCompleto);
        });
    }

    public static void ConfigurarEventos(this ModelBuilder builder)
    {
        builder.Entity<Evento>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Id).ValueGeneratedOnAdd();

            e.Property(ev => ev.Nombre).HasMaxLength(255).IsRequired();

            e.Property(ev => ev.Slug).HasMaxLength(255);

            e.Property(ev => ev.FechaInicio).IsRequired();

            e.Property(ev => ev.FechaFin).IsRequired();

            e.Property(ev => ev.Ubicacion).HasMaxLength(500).IsRequired();

            e.HasIndex(ev => ev.Slug).IsUnique();
        });
    }

    public static void ConfigurarInscripciones(this ModelBuilder builder)
    {
        builder.Entity<Inscripcion>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedOnAdd();

            e.Property(i => i.PersonaId).IsRequired();
            e.Property(i => i.EventoId).IsRequired();

            e.Property(i => i.Estado).IsRequired();

            e.Property(i => i.Ciudad).HasMaxLength(255);
            e.Property(i => i.Departamento).HasMaxLength(255);
            e.Ignore(i => i.Localidad);

            e.Property(i => i.Servicios)
                .HasColumnType("jsonb");

            e.Property(i => i.DescripcionAlergia).HasMaxLength(500);

            e.HasOne(i => i.Persona)
                .WithMany(p => p.Inscripciones)
                .HasForeignKey(i => i.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Evento)
                .WithMany(ev => ev.Inscripciones)
                .HasForeignKey(i => i.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.GrupoFamiliar)
                .WithMany(g => g.Inscripciones)
                .HasForeignKey(i => i.GrupoFamiliarId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(i => i.RelacionConPersona)
                .WithMany()
                .HasForeignKey(i => i.RelacionConPersonaId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public static void ConfigurarGrupoFamiliar(this ModelBuilder builder)
    {
        builder.Entity<GrupoFamiliar>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).ValueGeneratedOnAdd();
        });
    }

    public static void ConfigurarUsuariosAdministradores(this ModelBuilder builder)
    {
        builder.Entity<UsuarioAdministrador>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();

            e.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();

            e.HasIndex(u => u.Email).IsUnique();

            e.Property(u => u.Nombre)
                .HasMaxLength(255);

            e.Property(u => u.Rol)
                .HasConversion<string>()
                .HasMaxLength(50);

            e.Property(u => u.Localidades)
                .HasColumnType("jsonb");
        });
    }
}