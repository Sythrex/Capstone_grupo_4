using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        // options.AccessDeniedPath = "/Account/AccessDenied"; --> permiso denegado? en que situacion?
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews();

var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TallerMecanicoContext>(opt =>
    opt.UseSqlServer(cs)
);
builder.Services.AddScoped<IPasswordHasher<usuario>, PasswordHasher<usuario>>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();