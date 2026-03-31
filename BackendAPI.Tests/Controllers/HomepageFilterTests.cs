using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Screening;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendAPI.Tests.Controllers
{
    public class HomepageFilterTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public HomepageFilterTests()
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

        private ScreeningsController BuildController()
        {
            return new ScreeningsController(_db);
        }

        [Fact]
        public void GetScreenings_NoFilter_ReturnsAll()
        {
            var movie = CreateMovie();
            var hall = CreateHall();
            _db.Movies.Add(movie);
            _db.Halls.Add(hall);
            _db.Screenings.Add(CreateScreening(movie.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1)));
            _db.Screenings.Add(CreateScreening(movie.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(2)));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreenings();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(2, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public void GetScreenings_FilterByMovieId_ReturnsOnlyThatMovie()
        {
            var movie1 = CreateMovie("Movie One");
            var movie2 = CreateMovie("Movie Two");
            var hall = CreateHall();
            _db.Movies.AddRange(movie1, movie2);
            _db.Halls.Add(hall);
            _db.Screenings.Add(CreateScreening(movie1.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1)));
            _db.Screenings.Add(CreateScreening(movie2.MovieId, hall.HallId, DateTimeOffset.UtcNow.AddDays(1)));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreenings(movieId: movie1.MovieId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public void GetScreenings_FilterByDate_ReturnsOnlyThatDay()
        {
            var movie = CreateMovie();
            var hall = CreateHall();
            _db.Movies.Add(movie);
            _db.Halls.Add(hall);

            var targetDate = new DateTime(2026, 6, 15);
            _db.Screenings.Add(CreateScreening(movie.MovieId, hall.HallId, new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.Zero)));
            _db.Screenings.Add(CreateScreening(movie.MovieId, hall.HallId, new DateTimeOffset(2026, 6, 16, 14, 0, 0, TimeSpan.Zero)));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreenings(date: targetDate);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public void GetScreenings_FilterByMovieIdAndDate_ReturnsCombined()
        {
            var movie1 = CreateMovie("Movie One");
            var movie2 = CreateMovie("Movie Two");
            var hall = CreateHall();
            _db.Movies.AddRange(movie1, movie2);
            _db.Halls.Add(hall);

            var targetDate = new DateTime(2026, 7, 1);
            _db.Screenings.Add(CreateScreening(movie1.MovieId, hall.HallId, new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero)));
            _db.Screenings.Add(CreateScreening(movie1.MovieId, hall.HallId, new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero)));
            _db.Screenings.Add(CreateScreening(movie2.MovieId, hall.HallId, new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero)));
            _db.SaveChanges();

            var controller = BuildController();

            var result = controller.GetScreenings(movieId: movie1.MovieId, date: targetDate);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public void GetScreenings_NoMatches_ReturnsEmptyList()
        {
            var controller = BuildController();

            var result = controller.GetScreenings(movieId: Guid.NewGuid());

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, _jsonOptions);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }
    }
}
