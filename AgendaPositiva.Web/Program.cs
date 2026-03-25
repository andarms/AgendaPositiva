using AgendaPositiva.Web.Datos;
using AgendaPositiva.Web.Features.Inscripciones;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();


builder.Services.AddControllersWithViews().AddRazorOptions(o =>
{
    o.ViewLocationFormats.Clear();
    o.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/admin/auth/login";
    options.AccessDeniedPath = "/admin/auth/login";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPanel", policy => policy.RequireRole("Administrador", "Colaborador"));

builder.Services.AgregarModuloInscripciones();

var app = builder.Build();

{
    // Aplicar migraciones pendientes al iniciar
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
    {
        await DatosIniciales.AlimentarAsync(db, app.Environment);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();




app.Run();
