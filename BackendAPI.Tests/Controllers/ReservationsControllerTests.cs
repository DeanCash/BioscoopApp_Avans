using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Order;
using BackendAPI.Models.Reservation;
using BackendAPI.Models.Screening;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests.Controllers;

public class ReservationsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ReservationsController _sut;

    private readonly DateTimeOffset _now = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public ReservationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new ReservationsController(null!, _db);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedOrderAsync(string printCode)
    {
        var hall = new HallModel { HallId = Guid.NewGuid(), Number = 2, CreatedAtUtc = _now };
        var movie = new MovieModel
        {
            MovieId = Guid.NewGuid(), Title = "Test Film", Description = "",
            Genre = "Actie", DurationMinutes = 120, Age = 12, CreatedAtUtc = _now
        };
        var screening = new ScreeningModel
        {
            ScreeningId = Guid.NewGuid(), MovieId = movie.MovieId, HallId = hall.HallId,
            Movie = movie, Hall = hall, StartTimeUtc = _now.AddDays(1), CreatedAtUtc = _now
        };
        var order = new OrderModel
        {
            OrderId = Guid.NewGuid(),
            ScreeningId = screening.ScreeningId,
            Screening = screening,
            PrintCode = printCode,
            TotalAmount = 12.50m,
            PaymentStatus = "Paid",
            Status = "Confirmed",
            CreatedAtUtc = _now
        };

        _db.Halls.Add(hall);
        _db.Movies.Add(movie);
        _db.Screenings.Add(screening);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByPrintCode_KnownCode_ReturnsPrintCode()
    {
        await SeedOrderAsync("ABC123");

        var result = await _sut.GetByPrintCode("ABC123");

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ReservationGroupResponseDto>(ok.Value);
        Assert.Equal("ABC123", dto.PrintCode);
    }

    [Fact]
    public async Task GetByPrintCode_UnknownCode_ReturnsNotFound()
    {
        var result = await _sut.GetByPrintCode("ONBEKEND");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
