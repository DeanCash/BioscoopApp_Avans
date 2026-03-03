using BackendAPI.DTOs.Movies;
using BackendAPI.Services.Movies;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieQueryService _movieQueryService;

        public MoviesController(IMovieQueryService movieQueryService)
        {
            _movieQueryService = movieQueryService;
        }

        // GET api/movies/upcoming?daysAhead=14
        [HttpGet("upcoming")]
        public async Task<ActionResult<IReadOnlyList<UpcomingMovieDto>>> GetUpcomingMovies(
            [FromQuery] int daysAhead = 14,
            CancellationToken ct = default)
        {
            if (daysAhead < 1) daysAhead = 1;
            if (daysAhead > 365) daysAhead = 365;

            var result = await _movieQueryService.GetUpcomingMoviesAsync(
                DateTimeOffset.UtcNow,
                daysAhead,
                ct: ct);

            return Ok(result);
        }
    }
}
