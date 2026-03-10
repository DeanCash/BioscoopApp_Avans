using API.Services;
using BackendAPI.Services.Movies;
using BackendAPI.Services;
using BackendAPI.Models.User;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5100",
                "https://localhost:7076")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddScoped<IMovieQueryService, MovieQueryService>();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // If on "MacOS"
    if (OperatingSystem.IsMacOS())
    {
        var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
        options.UseMySQL(connectionString);
    }
    // If on "Windows"
    else
    {
        options.UseSqlServer("name=DefaultConnection", (s) =>
        {
            s.EnableRetryOnFailure(3);
        });
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
    SeedUsers(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
