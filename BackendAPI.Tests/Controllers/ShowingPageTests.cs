using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Order;
using BackendAPI.Models.Screening;
using BackendAPI.Models.Seat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendAPI.Tests.Controllers
{
    public class ShowingPageTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public ShowingPageTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private MovieModel CreateMovie(string title = "Test Movie")
        {
            return new MovieModel
            {
                MovieId = Guid.NewGuid(),
                Title = title,
                Description = "A test movie",
                DurationMinutes = 120,
                Age = 12,
                Genre = "Action",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private HallModel CreateHall(int number = 1)
        {
            return new HallModel
            {
                HallId = Guid.NewGuid(),
                Number = number,
                LayoutType = LayoutType.Standard,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private ScreeningModel CreateScreening(Guid movieId, Guid hallId, DateTimeOffset startTime)
        {
            return new ScreeningModel
            {
                ScreeningId = Guid.NewGuid(),
                MovieId = movieId,
                HallId = hallId,
                StartTimeUtc = startTime,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private SeatModel CreateSeat(Guid hallId, string rowLabel, int seatNumber)
        {
            return new SeatModel
            {
                SeatId = Guid.NewGuid(),
                HallId = hallId,
                RowLabel = rowLabel,
                SeatNumber = seatNumber
            };
        }

        private OrderModel CreateOrder(Guid screeningId, Guid? seatId = null)
        {
            return new OrderModel
            {
                OrderId = Guid.NewGuid(),
                ScreeningId = screeningId,
                SeatId = seatId,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                PrintCode = Guid.NewGuid().ToString(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private ScreeningsController BuildController()
        {
            return new ScreeningsController(_db);
        }

        [Fact]
        public void GetScreening_ValidId_ReturnsScreening()
        {
            var movie = CreateMovie();
            var hall = CreateHall();
            var screening = CreateScreening(movie.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1));
            _db.Movies.Add(movie);
            _db.Halls.Add(hall);
            _db.Screenings.Add(screening);
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreening(screening.ScreeningId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(screening.ScreeningId.ToString(), doc.RootElement.GetProperty("ScreeningId").GetString());
        }

        [Fact]
        public void GetScreening_InvalidId_ReturnsNotFound()
        {
            var controller = BuildController();

            var result = controller.GetScreening(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetScreeningsOverview_ReturnsProjectedFields()
        {
            var movie = CreateMovie("Overview Movie");
            var hall = CreateHall(3);
            _db.Movies.Add(movie);
            _db.Halls.Add(hall);
            _db.Screenings.Add(CreateScreening(movie.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1)));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreeningsOverview();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            var first = doc.RootElement[0];
            Assert.Equal("Overview Movie", first.GetProperty("movieTitle").GetString());
            Assert.Equal(3, first.GetProperty("hallNumber").GetInt32());
        }

        [Fact]
        public void GetSeatsForScreening_ReturnsSeatsWithReservationStatus()
        {
            var movie = CreateMovie();
            var hall = CreateHall();
            var screening = CreateScreening(movie.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1));
            var seat1 = CreateSeat(hall.HallId, "A", 1);
            var seat2 = CreateSeat(hall.HallId, "A", 2);

            _db.Movies.Add(movie);
            _db.Halls.Add(hall);
            _db.Screenings.Add(screening);
            _db.Seats.AddRange(seat1, seat2);
            _db.SaveChanges();

            // Reserve seat1
            _db.Orders.Add(CreateOrder(screening.ScreeningId, seat1.SeatId));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetSeatsForScreening(screening.ScreeningId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            var seats = doc.RootElement;
            Assert.Equal(2, seats.GetArrayLength());

            // Find reserved and unreserved seats
            var reserved = seats.EnumerateArray().First(s => s.GetProperty("SeatId").GetString() == seat1.SeatId.ToString());
            var unreserved = seats.EnumerateArray().First(s => s.GetProperty("SeatId").GetString() == seat2.SeatId.ToString());
            Assert.True(reserved.GetProperty("IsReserved").GetBoolean());
            Assert.False(unreserved.GetProperty("IsReserved").GetBoolean());
        }

        [Fact]
        public void GetSeatsForScreening_InvalidScreening_ReturnsNotFound()
        {
            var controller = BuildController();

            var result = controller.GetSeatsForScreening(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
