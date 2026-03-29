using BackendAPI.Models.Movie;
using API.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendAPI.Services.Movies;

public interface ITmdbService
{
    Task<int> ImportPopularMoviesAsync(int pages = 3, CancellationToken ct = default);
}

public sealed class TmdbService : ITmdbService
{
    private readonly HttpClient _http;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TmdbService> _logger;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TmdbService(HttpClient http, ApplicationDbContext context,
        ILogger<TmdbService> logger, IConfiguration config)
    {
        _http = http;
        _context = context;
        _logger = logger;
        _apiKey = config["Tmdb:ApiKey"] ?? throw new InvalidOperationException("Tmdb:ApiKey is not configured.");
    }

    public async Task<int> ImportPopularMoviesAsync(int pages = 3, CancellationToken ct = default)
    {
        int imported = 0;

        for (int page = 1; page <= pages; page++)
        {
            var url = $"https://api.themoviedb.org/3/movie/popular?api_key={_apiKey}&language=nl-NL&page={page}";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB page {Page} returned {Status}", page, response.StatusCode);
                break;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<TmdbPageResult>(body, _jsonOptions);
            if (result?.Results is null) break;

            foreach (var tmdbMovie in result.Results)
            {
                if (string.IsNullOrWhiteSpace(tmdbMovie.Title)) continue;

                // Skip als de film al bestaat op basis van titel
                bool exists = _context.Movies.Any(m => m.Title == tmdbMovie.Title);
                if (exists) continue;

                // Haal genre-namen op via genre_ids (gebruik een lokale mapping)
                var genreName = ResolveGenre(tmdbMovie.GenreIds);

                var movie = new MovieModel
                {
                    MovieId = Guid.NewGuid(),
                    Title = tmdbMovie.Title,
                    Description = tmdbMovie.Overview ?? string.Empty,
                    DurationMinutes = 120, // TMDB popular endpoint geeft geen runtime; wordt 120 als standaard
                    Age = tmdbMovie.Adult ? 18 : 12,
                    Genre = genreName,
                    ImageUrl = tmdbMovie.PosterPath is not null
                        ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}"
                        : null,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                _context.Movies.Add(movie);
                imported++;
            }

            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("TMDB import klaar: {Count} films toegevoegd.", imported);
        return imported;
    }

    private static string ResolveGenre(IReadOnlyList<int>? ids)
    {
        if (ids is null || ids.Count == 0) return "Overig";

        return ids[0] switch
        {
            28 => "Actie",
            12 => "Avontuur",
            16 => "Animatie",
            35 => "Komedie",
            80 => "Misdaad",
            99 => "Documentaire",
            18 => "Drama",
            10751 => "Familie",
            14 => "Fantasy",
            36 => "Geschiedenis",
            27 => "Horror",
            10402 => "Muziek",
            9648 => "Mystery",
            10749 => "Romantiek",
            878 => "Sci-Fi",
            10770 => "TV-film",
            53 => "Thriller",
            10752 => "Oorlog",
            37 => "Western",
            _ => "Overig"
        };
    }

    // ?? TMDB response modellen ?????????????????????????????????????????????????

    private sealed class TmdbPageResult
    {
        public List<TmdbMovie>? Results { get; set; }
    }

    private sealed class TmdbMovie
    {
        public string? Title { get; set; }
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("genre_ids")]
        public List<int>? GenreIds { get; set; }

        public bool Adult { get; set; }
    }
}
