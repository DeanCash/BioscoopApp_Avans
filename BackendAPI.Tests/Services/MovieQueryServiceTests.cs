using API.Services;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Screening;
using BackendAPI.Services.Movies;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Tests.Services;

public class MovieQueryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly MovieQueryService _sut;

    private readonly DateTimeOffset _now = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public MovieQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new MovieQueryService(_db);
    }

    public void Dispose() => _db.Dispose();

    private HallModel CreateHall(int number = 1)
    {
        var hall = new HallModel
        {
            HallId = Guid.NewGuid(),
            Number = number,
            CreatedAtUtc = _now
        };
        _db.Halls.Add(hall);
        return hall;
    }

    private MovieModel CreateMovie(string title = "Test Film", string genre = "Actie", int age = 12)
    {
        var movie = new MovieModel
        {
            MovieId = Guid.NewGuid(),
            Title = title,
            Description = "Beschrijving",
            DurationMinutes = 120,
            Age = age,
            Genre = genre,
            CreatedAtUtc = _now
        };
        _db.Movies.Add(movie);
        return movie;
    }

    private ScreeningModel CreateScreening(MovieModel movie, HallModel hall, DateTimeOffset startTime)
    {
        var screening = new ScreeningModel
        {
            ScreeningId = Guid.NewGuid(),
            MovieId = movie.MovieId,
            HallId = hall.HallId,
            Movie = movie,
            Hall = hall,
            StartTimeUtc = startTime,
            CreatedAtUtc = _now
        };
        _db.Screenings.Add(screening);
        return screening;
    }

    [Fact]
    public async Task GetUpcomingMovies_ReturnsMoviesWithinFilmWeek()
    {
        var hall = CreateHall();
        var movie = CreateMovie("Binnen Filmweek");
        CreateScreening(movie, hall, _now.AddDays(2));
        await _db.SaveChangesAsync();

        var result = await _sut.GetUpcomingMoviesAsync(_now, daysAhead: 7);

        Assert.Single(result);
        Assert.Equal("Binnen Filmweek", result[0].Title);
    }

    [Fact]
    public async Task GetUpcomingMovies_ExcludesMoviesOutsideFilmWeek()
    {
        var hall = CreateHall();
        var movieBinnen = CreateMovie("Binnen");
        CreateScreening(movieBinnen, hall, _now.AddDays(3));
        var movieBuiten = CreateMovie("Buiten");
        CreateScreening(movieBuiten, hall, _now.AddDays(10));
        await _db.SaveChangesAsync();

        var result = await _sut.GetUpcomingMoviesAsync(_now, daysAhead: 7);

        Assert.Single(result);
        Assert.Equal("Binnen", result[0].Title);
    }

    [Fact]
    public async Task GetUpcomingMovies_ExcludesPastScreenings()
    {
        var hall = CreateHall();
        var movie = CreateMovie("Gisteren");
        CreateScreening(movie, hall, _now.AddDays(-1));
        await _db.SaveChangesAsync();

        var result = await _sut.GetUpcomingMoviesAsync(_now, daysAhead: 7);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUpcomingMovies_ReturnsEmptyList_WhenNoScreenings()
    {
        var result = await _sut.GetUpcomingMoviesAsync(_now, daysAhead: 7);

        Assert.Empty(result);
    }
}
