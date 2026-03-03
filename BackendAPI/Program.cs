using API.Services;
using BackendAPI.Services;
using BackendAPI.Models.User;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (OperatingSystem.IsMacOS())
    {
        var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
        options.UseMySQL(connectionString);
    }
    else
    {
        options.UseSqlServer("name=DefaultConnection");
    }
});

builder.Services.AddScoped<ReservationService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "CinemaAuth";
        o.LoginPath = "/admin-panel/login";     
        o.AccessDeniedPath = "/admin-panel/denied";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(db);
    SeedUsers(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void SeedUsers(ApplicationDbContext db)
{
    if (db.Users.Any()) return;

    var hasher = new PasswordHasher<UserModel>();

    var manager = new UserModel
    {
        Username = "manager",
        Role = "Manager"
    };
    manager.PasswordHash = hasher.HashPassword(manager, "Test123!");

    var cashier = new UserModel
    {
        Username = "cashier",
        Role = "Cashier"
    };
    cashier.PasswordHash = hasher.HashPassword(cashier, "Test123!");

    db.Users.AddRange(manager, cashier);
    db.SaveChanges();
}
