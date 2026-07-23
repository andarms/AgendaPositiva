using AgendaPositiva.Web.Features.Admin.Hospedajes.Dominio;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Features.Admin.Hospedajes;

public static class HospedajesDbContextExtensions
{
    public static void ConfigurarCasas(this ModelBuilder builder)
    {
        builder.Entity<Casa>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.Nombre).HasMaxLength(255).IsRequired();
            e.Property(c => c.Direccion).HasMaxLength(500);
            e.Property(c => c.Telefono).HasMaxLength(50);
            e.Property(c => c.NombreResponsable).HasMaxLength(255);
            e.Property(c => c.TelefonoResponsable).HasMaxLength(50);

            e.HasOne(c => c.ResponsablePersona)
                .WithMany()
                .HasForeignKey(c => c.ResponsablePersonaId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public static void ConfigurarHoteles(this ModelBuilder builder)
    {
        builder.Entity<Hotel>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.Nombre).HasMaxLength(255).IsRequired();
            e.Property(h => h.Direccion).HasMaxLength(500);
            e.Property(h => h.Telefono).HasMaxLength(50);
        });
    }

    public static void ConfigurarHabitacionesHotel(this ModelBuilder builder)
    {
        builder.Entity<HabitacionHotel>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.Nombre).HasMaxLength(255).IsRequired();

            e.HasOne(h => h.Hotel)
                .WithMany(h => h.Habitaciones)
                .HasForeignKey(h => h.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public static void ConfigurarAsignacionesHospedaje(this ModelBuilder builder)
    {
        builder.Entity<AsignacionHospedaje>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedOnAdd();
            e.HasIndex(a => a.InscripcionId).IsUnique();

            e.HasOne(a => a.Inscripcion)
                .WithMany()
                .HasForeignKey(a => a.InscripcionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Casa)
                .WithMany(c => c.Asignaciones)
                .HasForeignKey(a => a.CasaId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.HabitacionHotel)
                .WithMany(h => h.Asignaciones)
                .HasForeignKey(a => a.HabitacionHotelId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
