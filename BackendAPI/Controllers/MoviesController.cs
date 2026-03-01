using API.Services;
using BackendAPI.Models.Movie;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public MoviesController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public List<MovieModel> GetMovies()
        {
            return context.Movies.OrderByDescending(c => c.MovieId).ToList();
        }

        [HttpGet("{id}")]
        public IActionResult GetMovie(int id)
        {
            var movie = context.Movies.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            return Ok(movie);
        }

        [HttpPost]
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
                CreatedAtUtc = DateTime.Now,
            };

            context.Movies.Add(movie);
            context.SaveChanges();

            return Ok(movie);
        }

        [HttpPut("{id}")]
        public IActionResult EditMovie(int id, MovieDto movieDto)
        {
            var otherMovie = context.Movies.FirstOrDefault(c => c.Title == movieDto.Title);
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

            context.SaveChanges();

            return Ok(movie);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMovie(int id)
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
    }
}
