using API.Services;
using BackendAPI.DTOs.Movies;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Services.Movies;

public interface IMovieQueryService
{
    Task<IReadOnlyList<UpcomingMovieDto>> GetUpcomingMoviesAsync(
        DateTimeOffset fromUtc,
        int daysAhead = 14,
        int maxMovies = 50,
        int maxScreeningsPerMovie = 10,
        CancellationToken ct = default);

    Task<MovieDetailsDto?> GetMovieDetailsAsync(
        Guid movieId,
        DateTimeOffset fromUtc,
        int daysAhead = 30,
        CancellationToken ct = default);
}

public sealed class MovieQueryService : IMovieQueryService
{
    private readonly ApplicationDbContext _context;

    public MovieQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UpcomingMovieDto>> GetUpcomingMoviesAsync(
        DateTimeOffset fromUtc,
        int daysAhead = 14,
        int maxMovies = 50,
        int maxScreeningsPerMovie = 10,
        CancellationToken ct = default)
    {
        var untilUtc = fromUtc.AddDays(daysAhead);

        // 1) Haal alle screenings op + movie en hall navigatie, dan filter in memory
        // (SQLite heeft problemen met DateTimeOffset vergelijkingen in LINQ)
        var allScreenings = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .ToListAsync(ct);

        var screenings = allScreenings
            .Where(s => s.StartTimeUtc >= fromUtc && s.StartTimeUtc <= untilUtc)
            .OrderBy(s => s.StartTimeUtc)
            .ToList();

        // 2) Groepeer per film en map naar DTO
        var result = screenings
            .GroupBy(s => s.MovieId)
            .Select(g =>
            {
                var movie = g.First().Movie;

                return new UpcomingMovieDto
                {
                    MovieId = movie.MovieId,
                    Title = movie.Title,
                    ImageUrl = movie.ImageUrl,
                    FirstScreeningAtUtc = g.Min(x => x.StartTimeUtc),
                    Screenings = g
                        .OrderBy(x => x.StartTimeUtc)
                        .Take(maxScreeningsPerMovie)
                        .Select(x => new UpcomingScreeningDto
                        {
                            ScreeningId = x.ScreeningId,
                            StartTimeUtc = x.StartTimeUtc,
                            HallId = x.HallId,
                            HallNumber = x.Hall.Number
                        })
                        .ToList()
                };
            })
            .OrderBy(x => x.FirstScreeningAtUtc)
            .Take(maxMovies)
            .ToList();

        return result;
    }

    public async Task<MovieDetailsDto?> GetMovieDetailsAsync(
        Guid movieId,
        DateTimeOffset fromUtc,
        int daysAhead = 30,
        CancellationToken ct = default)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieId == movieId, ct);

        if (movie == null)
            return null;

        var untilUtc = fromUtc.AddDays(daysAhead);

        // Haal screenings met hall info
        var allScreenings = await _context.Screenings
            .AsNoTracking()
            .Include(s => s.Hall)
                .ThenInclude(h => h.Seats)
            .Where(s => s.MovieId == movieId)
            .ToListAsync(ct);

        var screenings = allScreenings
            .Where(s => s.StartTimeUtc >= fromUtc && s.StartTimeUtc <= untilUtc)
            .OrderBy(s => s.StartTimeUtc)
            .ToList();

        // Haal bezette stoelen per screening (via Orders met een SeatId)
        var screeningIds = screenings.Select(s => s.ScreeningId).ToList();
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => screeningIds.Contains(o.ScreeningId) && o.SeatId != null)
            .GroupBy(o => o.ScreeningId)
            .Select(g => new { ScreeningId = g.Key, ReservedCount = g.Count() })
            .ToListAsync(ct);

        var reservationDict = orders.ToDictionary(o => o.ScreeningId, o => o.ReservedCount);

        return new MovieDetailsDto
        {
            MovieId = movie.MovieId,
            Title = movie.Title,
            Description = movie.Description,
            DurationMinutes = movie.DurationMinutes,
            Age = movie.Age,
            ImageUrl = movie.ImageUrl,
            Screenings = screenings.Select(s =>
            {
                var totalSeats = s.Hall.Seats.Count;
                var reserved = reservationDict.GetValueOrDefault(s.ScreeningId, 0);
                return new ScreeningWithHallDto
                {
                    ScreeningId = s.ScreeningId,
                    StartTimeUtc = s.StartTimeUtc,
                    HallId = s.HallId,
                    HallNumber = s.Hall.Number,
                    HallName = $"Zaal {s.Hall.Number}",
                    TotalSeats = totalSeats,
                    AvailableSeats = totalSeats - reserved
                };
            }).ToList()
        };
    }
}