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

        // 1) Haal toekomstige screenings op + movie navigatie
        var screenings = await _context.Screenings
            .AsNoTracking()
            .Where(s => s.StartTimeUtc >= fromUtc && s.StartTimeUtc <= untilUtc)
            .Include(s => s.Movie)
            .OrderBy(s => s.StartTimeUtc)
            .ToListAsync(ct);

        // 2) Groepeer per film en map naar DTO
        var result = screenings
            .GroupBy(s => s.MovieId)
            .Select(g =>
            {
                var movie = g.First().Movie;

                return new UpcomingMovieDto
                {
                    MovieId = movie.MovieId, // let op: jouw MovieModel gebruikt MovieId (Guid)
                    Title = movie.Title,
                    FirstScreeningAtUtc = g.Min(x => x.StartTimeUtc),
                    Screenings = g
                        .OrderBy(x => x.StartTimeUtc)
                        .Take(maxScreeningsPerMovie)
                        .Select(x => new UpcomingScreeningDto
                        {
                            ScreeningId = x.ScreeningId,
                            StartTimeUtc = x.StartTimeUtc,
                            HallId = x.HallId
                        })
                        .ToList()
                };
            })
            .OrderBy(x => x.FirstScreeningAtUtc)
            .Take(maxMovies)
            .ToList();

        return result;
    }
}