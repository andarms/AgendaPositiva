using AgendaPositiva.Web.Features.Admin.Servicios.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Servicios;

public static class ServiciosDbContextExtensions
{
    public static void ConfigurarServicios(this ModelBuilder builder)
    {
        builder.Entity<Servicio>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedOnAdd();
            e.Property(s => s.Nombre).HasMaxLength(255).IsRequired();
        });
    }

    public static void ConfigurarHorariosServicio(this ModelBuilder builder)
    {
        builder.Entity<HorarioServicio>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.Descripcion).HasMaxLength(255).IsRequired();

            e.HasOne(h => h.Servicio)
                .WithMany(s => s.Horarios)
                .HasForeignKey(h => h.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public static void ConfigurarGruposServicio(this ModelBuilder builder)
    {
        builder.Entity<GrupoServicio>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).ValueGeneratedOnAdd();
            e.Property(g => g.Nombre).HasMaxLength(255).IsRequired();

            e.HasOne(g => g.Servicio)
                .WithMany(s => s.Grupos)
                .HasForeignKey(g => g.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.HorarioServicio)
                .WithMany(h => h.Grupos)
                .HasForeignKey(g => g.HorarioServicioId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(g => g.LiderInscripcion)
                .WithMany()
                .HasForeignKey(g => g.LiderInscripcionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public static void ConfigurarMiembrosGrupoServicio(this ModelBuilder builder)
    {
        builder.Entity<MiembroGrupoServicio>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.HasOne(m => m.GrupoServicio)
                .WithMany(g => g.Miembros)
                .HasForeignKey(m => m.GrupoServicioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Inscripcion)
                .WithMany()
                .HasForeignKey(m => m.InscripcionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => new { m.GrupoServicioId, m.InscripcionId }).IsUnique();
        });
    }
}
