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
                .ToList()
                .OrderBy(s => s.StartTimeUtc)
                .ToList();

            return Ok(screenings);
        }

        [HttpGet("overview")]
        [AllowAnonymous]
        public IActionResult GetScreeningsOverview()
        {
            var screenings = context.Screenings
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .ToList()
                .OrderBy(s => s.StartTimeUtc)
                .Select(s => new
                {
                    screeningId = s.ScreeningId,
                    movieTitle = s.Movie.Title,
                    hallNumber = s.Hall.Number,
                    startTimeUtc = s.StartTimeUtc
                })
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
        
        [HttpGet("available-seats")]
        [AllowAnonymous]
        public IActionResult GetAvailableSeats([FromQuery] Guid movieId)
        {
            var screenings = context.Screenings
                .AsNoTracking()
                .Where(s => s.MovieId == movieId)
                .Include(s => s.Hall)
                .ThenInclude(h => h.Seats)
                .ToList();

            // Count reserved orders per screening using a join instead of Contains
            var reservedByScreening = context.Orders
                .AsNoTracking()
                .Join(
                    context.Screenings.Where(s => s.MovieId == movieId),
                    o => o.ScreeningId,
                    s => s.ScreeningId,
                    (o, s) => o.ScreeningId
                )
                .GroupBy(id => id)
                .Select(g => new { ScreeningId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.ScreeningId, x => x.Count);

            var result = screenings.ToDictionary(
                s => s.ScreeningId,
                s => s.Hall.Seats.Count - reservedByScreening.GetValueOrDefault(s.ScreeningId, 0)
            );

            return Ok(result);
        }


        [HttpGet("{id}/seats")]
        [AllowAnonymous]
        public IActionResult GetSeatsForScreening(Guid id)
        {
            var screening = context.Screenings
                .AsNoTracking()
                .FirstOrDefault(s => s.ScreeningId == id);

            if (screening == null) return NotFound();

            var seats = context.Seats
                .AsNoTracking()
                .Where(s => s.HallId == screening.HallId)
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .ToList();

            var reservedSeatIds = context.Orders
                .AsNoTracking()
                .Where(o => o.ScreeningId == id && o.SeatId != null
                    && o.PaymentStatus != "pending")
                .Select(o => o.SeatId)
                .ToHashSet();

            var result = seats.Select(s => new
            {
                s.SeatId,
                s.RowLabel,
                s.SeatNumber,
                IsReserved = reservedSeatIds.Contains(s.SeatId)
            });

            return Ok(result);
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