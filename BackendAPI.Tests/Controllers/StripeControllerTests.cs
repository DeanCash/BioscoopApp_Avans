using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Order;
using BackendAPI.Models.Screening;
using BackendAPI.Services.Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe.Checkout;

namespace BackendAPI.Tests.Controllers;

public class StripeControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IStripeSessionFetcher> _sessionFetcher;
    private readonly StripeController _sut;

    private readonly DateTimeOffset _now = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public StripeControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sessionFetcher = new Mock<IStripeSessionFetcher>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Stripe:SecretKey"] = "sk_test_fake" })
            .Build();

        _sut = new StripeController(_db, config, _sessionFetcher.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task<OrderModel> SeedOrderAsync(string printCode, decimal amount, string paymentStatus = "pending")
    {
        var hall = new HallModel { HallId = Guid.NewGuid(), Number = 1, CreatedAtUtc = _now };
        var movie = new MovieModel
        {
            MovieId = Guid.NewGuid(), Title = "Test Film", Description = "", Genre = "Actie",
            DurationMinutes = 120, Age = 12, CreatedAtUtc = _now
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
            TotalAmount = amount,
            PaymentStatus = paymentStatus,
            Status = "Pending",
            CreatedAtUtc = _now
        };

        _db.Halls.Add(hall);
        _db.Movies.Add(movie);
        _db.Screenings.Add(screening);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }

    private Session BuildSession(string printCode, string paymentStatus = "paid")
    {
        return new Session
        {
            PaymentStatus = paymentStatus,
            Metadata = new Dictionary<string, string> { ["printCode"] = printCode }
        };
    }

    [Fact]
    public async Task ConfirmPayment_PaidSession_MarksOrdersAsPaid()
    {
        await SeedOrderAsync("ABC123", 12.50m);
        var session = BuildSession("ABC123");
        _sessionFetcher.Setup(s => s.GetAsync("cs_test_fake", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _sut.ConfirmPayment(
            new StripeController.ConfirmPaymentRequest("cs_test_fake"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var order = _db.Orders.Single(o => o.PrintCode == "ABC123");
        Assert.Equal("Paid", order.PaymentStatus);
        Assert.Equal("Confirmed", order.Status);
        Assert.NotNull(order.PaidAtUtc);
    }

    [Fact]
    public async Task ConfirmPayment_UnpaidSession_ReturnsBadRequest()
    {
        await SeedOrderAsync("XYZ999", 10.00m);
        var session = BuildSession("XYZ999", paymentStatus: "unpaid");
        _sessionFetcher.Setup(s => s.GetAsync("cs_test_fake", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _sut.ConfirmPayment(
            new StripeController.ConfirmPaymentRequest("cs_test_fake"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var order = _db.Orders.Single(o => o.PrintCode == "XYZ999");
        Assert.NotEqual("Paid", order.PaymentStatus);
    }

    [Fact]
    public async Task ConfirmPayment_InvalidSession_ReturnsBadRequest()
    {
        _sessionFetcher.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Stripe fout"));

        var result = await _sut.ConfirmPayment(
            new StripeController.ConfirmPaymentRequest("cs_invalid"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCheckoutSession_UnknownPrintCode_ReturnsNotFound()
    {
        var request = new StripeController.CreateCheckoutRequest(
            PrintCode: "ONBEKEND",
            ScreeningId: Guid.NewGuid(),
            FrontendBaseUrl: "https://localhost:7076");

        var result = await _sut.CreateCheckoutSession(request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
