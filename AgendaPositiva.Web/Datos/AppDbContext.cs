using AgendaPositiva.Web.Features.Admin.Auth.Domain;
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
    public DbSet<UsuarioSistema> UsuariosSistema => Set<UsuarioSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigurarPersonas();
        modelBuilder.ConfigurarEventos();
        modelBuilder.ConfigurarInscripciones();
        modelBuilder.ConfigurarGrupoFamiliar();
        modelBuilder.ConfigurarUsuariosSistema();
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
