using AgendaPositiva.Web.Datos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();


builder.Services.AddControllersWithViews().AddRazorOptions(o => {
    o.ViewLocationFormats.Clear();
    o.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Alimentar la base de datos con datos iniciales
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatosIniciales.AlimentarAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
