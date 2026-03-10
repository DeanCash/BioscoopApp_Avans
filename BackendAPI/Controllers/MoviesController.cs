using API.Services;
using BackendAPI.DTOs.Movies;
using BackendAPI.Models.Movie;
using BackendAPI.Services.Movies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMovieQueryService _movieQueryService;

        public MoviesController(ApplicationDbContext context, IMovieQueryService movieQueryService)
        {
            this.context = context;
            _movieQueryService = movieQueryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public List<MovieModel> GetMovies()
        {
            return context.Movies.OrderByDescending(c => c.MovieId).ToList();
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetMovie(Guid id)
        {
            var movie = context.Movies.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            return Ok(movie);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult CreateMovie(MovieDto movieDto)
        {
            var otherMovie = context.Movies.FirstOrDefault(c => c.Title == movieDto.Title);
            if (otherMovie != null)
            {
                ModelState.AddModelError("Title", "The movie already exists in the database");
                var validation = new ValidationProblemDetails(ModelState);
                return BadRequest(validation);
            }

            var movie = new MovieModel
            {
                Title = movieDto.Title,
                Description = movieDto.Description,
                DurationMinutes = movieDto.DurationMinutes,
                Age = movieDto.Age,
                Genre = movieDto.Genre,
                ImageUrl = movieDto.ImageUrl,
                CreatedAtUtc = DateTime.Now,
            };

            context.Movies.Add(movie);
            context.SaveChanges();

            return Ok(movie);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult EditMovie(Guid id, MovieDto movieDto)
        {
            var otherMovie = context.Movies.FirstOrDefault(c => c.Title == movieDto.Title && c.MovieId != id);
            if (otherMovie != null)
            {
                ModelState.AddModelError("Title", "The movie already exists in the database");
                var validation = new ValidationProblemDetails(ModelState);
                return BadRequest(validation);
            }

            var movie = context.Movies.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            movie.Title = movieDto.Title;
            movie.Description = movieDto.Description;
            movie.DurationMinutes = movieDto.DurationMinutes;
            movie.Age = movieDto.Age;
            movie.Genre = movieDto.Genre;
            movie.ImageUrl = movieDto.ImageUrl;

            context.SaveChanges();

            return Ok(movie);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult DeleteMovie(Guid id)
        {
            var movie = context.Movies.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            context.Movies.Remove(movie);
            context.SaveChanges();

            return Ok(movie);
        }

        // GET api/movies/upcoming?daysAhead=14
        [HttpGet("upcoming")]
        [AllowAnonymous]
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

        // GET api/movies/{id}/details
        [HttpGet("{id}/details")]
        [AllowAnonymous]
        public async Task<ActionResult<MovieDetailsDto>> GetMovieDetails(
            Guid id,
            [FromQuery] int daysAhead = 30,
            CancellationToken ct = default)
        {
            if (daysAhead < 1) daysAhead = 1;
            if (daysAhead > 365) daysAhead = 365;

            var result = await _movieQueryService.GetMovieDetailsAsync(
                id,
                DateTimeOffset.UtcNow,
                daysAhead,
                ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
