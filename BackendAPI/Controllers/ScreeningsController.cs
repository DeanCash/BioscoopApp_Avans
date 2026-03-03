using API.Services;
using BackendAPI.Models.Screening;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScreeningsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public ScreeningsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetScreenings([FromQuery] Guid? movieId = null, [FromQuery] DateTime? date = null)
        {
            var q = context.Screenings
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .AsQueryable();

            if (movieId.HasValue)
                q = q.Where(s => s.MovieId == movieId.Value);

            if (date.HasValue)
            {
                var start = new DateTimeOffset(date.Value.Date, TimeSpan.Zero);
                var end = start.AddDays(1);
                q = q.Where(s => s.StartTimeUtc >= start && s.StartTimeUtc < end);
            }

            var screenings = q
                .OrderBy(s => s.StartTimeUtc)
                .ToList();

            return Ok(screenings);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetScreening(Guid id)
        {
            var screening = context.Screenings
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefault(s => s.ScreeningId == id);

            if (screening == null) return NotFound();
            return Ok(screening);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult CreateScreening(ScreeningDto dto)
        {
            var movieExists = context.Movies.Any(m => m.MovieId == dto.MovieId);
            if (!movieExists)
            {
                ModelState.AddModelError("MovieId", "Movie does not exist.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var hallExists = context.Halls.Any(h => h.HallId == dto.HallId);
            if (!hallExists)
            {
                ModelState.AddModelError("HallId", "Hall does not exist.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var duplicate = context.Screenings.Any(s =>
                s.MovieId == dto.MovieId &&
                s.HallId == dto.HallId &&
                s.StartTimeUtc == dto.StartTimeUtc);

            if (duplicate)
            {
                ModelState.AddModelError("StartTimeUtc", "Screening already exists for this hall and time.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var screening = new ScreeningModel
            {
                ScreeningId = Guid.NewGuid(),
                MovieId = dto.MovieId,
                HallId = dto.HallId,
                StartTimeUtc = dto.StartTimeUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            context.Screenings.Add(screening);
            context.SaveChanges();

            return Ok(screening);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult EditScreening(Guid id, ScreeningDto dto)
        {
            var screening = context.Screenings.Find(id);
            if (screening == null) return NotFound();

            var movieExists = context.Movies.Any(m => m.MovieId == dto.MovieId);
            if (!movieExists)
            {
                ModelState.AddModelError("MovieId", "Movie does not exist.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var hallExists = context.Halls.Any(h => h.HallId == dto.HallId);
            if (!hallExists)
            {
                ModelState.AddModelError("HallId", "Hall does not exist.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var duplicate = context.Screenings.Any(s =>
                s.ScreeningId != id &&
                s.MovieId == dto.MovieId &&
                s.HallId == dto.HallId &&
                s.StartTimeUtc == dto.StartTimeUtc);

            if (duplicate)
            {
                ModelState.AddModelError("StartTimeUtc", "Screening already exists for this hall and time.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            screening.MovieId = dto.MovieId;
            screening.HallId = dto.HallId;
            screening.StartTimeUtc = dto.StartTimeUtc;

            context.SaveChanges();

            return Ok(screening);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult DeleteScreening(Guid id)
        {
            var screening = context.Screenings.Find(id);
            if (screening == null) return NotFound();

            context.Screenings.Remove(screening);
            context.SaveChanges();

            return Ok(screening);
        }
    }
}