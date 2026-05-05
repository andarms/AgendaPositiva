using AgendaPositiva.Web.Features.Admin.Auth.Domain;
using AgendaPositiva.Web.Features.Admin.Auditoria;
using AgendaPositiva.Web.Features.Admin.Regiones.Dominio;
using AgendaPositiva.Web.Features.Admin.Servicios;
using AgendaPositiva.Web.Features.Admin.Servicios.Dominio;
using AgendaPositiva.Web.Features.Commons.Domain;
using AgendaPositiva.Web.Features.Inscripciones.Dominio;
using AgendaPositiva.Web.Features.Inscripciones.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace AgendaPositiva.Web.Datos;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<GrupoFamiliar> GrupoFamiliar => Set<GrupoFamiliar>();
    public DbSet<UsuarioAdministrador> UsuariosAdministradores => Set<UsuarioAdministrador>();
    public DbSet<RegionEvento> RegionesEvento => Set<RegionEvento>();
    public DbSet<UsuarioRegion> UsuarioRegiones => Set<UsuarioRegion>();
    public DbSet<AuditoriaAdmin> AuditoriaAdmin => Set<AuditoriaAdmin>();
    public DbSet<CategoriaInscripcion> CategoriasInscripcion => Set<CategoriaInscripcion>();
    public DbSet<AbonoInscripcion> AbonosInscripcion => Set<AbonoInscripcion>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<HorarioServicio> HorariosServicio => Set<HorarioServicio>();
    public DbSet<GrupoServicio> GruposServicio => Set<GrupoServicio>();
    public DbSet<MiembroGrupoServicio> MiembrosGrupoServicio => Set<MiembroGrupoServicio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigurarPersonas();
        modelBuilder.ConfigurarEventos();
        modelBuilder.ConfigurarInscripciones();
        modelBuilder.ConfigurarGrupoFamiliar();
        modelBuilder.ConfigurarUsuariosAdministradores();
        modelBuilder.ConfigurarRegionesEvento();
        modelBuilder.ConfigurarUsuarioRegiones();
        modelBuilder.ConfigurarCategoriasInscripcion();
        modelBuilder.ConfigurarAbonosInscripcion();
        modelBuilder.ConfigurarServicios();
        modelBuilder.ConfigurarHorariosServicio();
        modelBuilder.ConfigurarGruposServicio();
        modelBuilder.ConfigurarMiembrosGrupoServicio();

        modelBuilder.Entity<AuditoriaAdmin>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedOnAdd();
            e.Property(a => a.Usuario).HasMaxLength(255).IsRequired();
            e.Property(a => a.Accion).HasMaxLength(255).IsRequired();
            e.Property(a => a.ValorAnterior).HasMaxLength(500);
            e.Property(a => a.ValorNuevo).HasMaxLength(500);
        });
    }

    public override int SaveChanges()
    {
        LogAuditoria();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        LogAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void LogAuditoria()
    {
        var entries = ChangeTracker.Entries<EntidadBase>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.FechaCreacion = now;
                entry.Entity.FechaActualizacion = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.FechaActualizacion = now;
            }
        }
    }
}
