using API.Services;
using BackendAPI.Controllers;
using BackendAPI.DTOs.Orders;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Order;
using BackendAPI.Models.Screening;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackendAPI.Tests.Controllers
{
    public class OrdersControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public OrdersControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private ScreeningModel CreateScreening(DateTimeOffset start)
        {
            var movie = new MovieModel
            {
                MovieId = Guid.NewGuid(),
                Title = "Test Movie",
                Description = "",
                Genre = "Drama",
                DurationMinutes = 100,
                Age = 12,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var hall = new HallModel
            {
                HallId = Guid.NewGuid(),
                Number = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var screening = new ScreeningModel
            {
                ScreeningId = Guid.NewGuid(),
                MovieId = movie.MovieId,
                HallId = hall.HallId,
                Movie = movie,
                Hall = hall,
                StartTimeUtc = start,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            return screening;
        }

        private OrderModel CreateOrder(Guid screeningId, decimal total)
        {
            return new OrderModel
            {
                OrderId = Guid.NewGuid(),
                ScreeningId = screeningId,
                Status = "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = "Pin",
                TotalAmount = total,
                PrintCode = "TESTCODE",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private OrdersController BuildController()
        {
            return new OrdersController(_db);
        }

        [Fact]
        public async Task CreateOrder_ValidRequest_CreatesOrder_ReturnsCreated()
        {
            var screening = CreateScreening(DateTimeOffset.UtcNow.AddDays(1));
            _db.Movies.Add(screening.Movie);
            _db.Halls.Add(screening.Hall);
            _db.Screenings.Add(screening);
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var request = new CreateOrderRequest
            {
                ScreeningId = screening.ScreeningId,
                TotalAmount = 10.50m
            };

            var result = await controller.CreateOrder(request);

            var created = Assert.IsType<CreatedResult>(result.Result);
            var json = JsonSerializer.Serialize(created.Value);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(10.50m, doc.RootElement.GetProperty("TotalAmount").GetDecimal());
            Assert.Equal("Pending", doc.RootElement.GetProperty("PaymentStatus").GetString());
            Assert.Equal("Pending", doc.RootElement.GetProperty("Status").GetString());
            Assert.Equal("Pin", doc.RootElement.GetProperty("PaymentMethod").GetString());

            // Confirm the order exists in the database
            var idString = created.Location?.Split('/').Last();
            Assert.True(Guid.TryParse(idString, out var createdId));
            var orderInDb = await _db.Orders.FindAsync(createdId);
            Assert.NotNull(orderInDb);
            Assert.Equal(10.50m, orderInDb!.TotalAmount);
        }

        [Fact]
        public async Task CreateOrder_InvalidScreening_ReturnsBadRequest()
        {
            var controller = BuildController();

            var request = new CreateOrderRequest
            {
                ScreeningId = Guid.NewGuid(),
                TotalAmount = 5.00m
            };

            var result = await controller.CreateOrder(request);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Voorstelling niet gevonden.", bad.Value);
        }

        [Fact]
        public async Task ConfirmPayment_ValidOrder_ReturnsOkAndUpdates()
        {
            var screening = CreateScreening(DateTimeOffset.UtcNow.AddDays(1));
            _db.Movies.Add(screening.Movie);
            _db.Halls.Add(screening.Hall);
            _db.Screenings.Add(screening);

            var order = CreateOrder(screening.ScreeningId, 7.75m);
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.ConfirmPayment(order.OrderId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal("Paid", doc.RootElement.GetProperty("PaymentStatus").GetString());
            Assert.Equal("Confirmed", doc.RootElement.GetProperty("Status").GetString());

            // Verify DB updated
            var updated = await _db.Orders.FindAsync(order.OrderId);
            Assert.NotNull(updated);
            Assert.Equal("Paid", updated!.PaymentStatus);
            Assert.Equal("Confirmed", updated.Status);
            Assert.True(updated.PaidAtUtc.HasValue);
        }

        [Fact]
        public async Task ConfirmPayment_InvalidId_ReturnsNotFound()
        {
            var controller = BuildController();

            var result = await controller.ConfirmPayment(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
