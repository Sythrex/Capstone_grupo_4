using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(cs)                    // Usa SQL Server
);

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


//dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:<tu-servidor>.database.windows.net,1433;Initial Catalog=<tu_bd>;User ID=<tu_usuario>;Password=<tu_password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
