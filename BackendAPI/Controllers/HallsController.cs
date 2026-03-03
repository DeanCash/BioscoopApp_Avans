using API.Services;
using BackendAPI.Models.Hall;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HallsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public HallsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetHalls()
        {
            var halls = context.Halls
                .OrderBy(h => h.Number)
                .ToList();

            return Ok(halls);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetHall(Guid id)
        {
            var hall = context.Halls.Find(id);
            if (hall == null) return NotFound();
            return Ok(hall);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult CreateHall(HallDto dto)
        {
            var existing = context.Halls.FirstOrDefault(h => h.Number == dto.Number);
            if (existing != null)
            {
                ModelState.AddModelError("Number", "Hall number already exists.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var hall = new HallModel
            {
                HallId = Guid.NewGuid(),
                Number = dto.Number,
                LayoutType = dto.LayoutType,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            context.Halls.Add(hall);
            context.SaveChanges();

            return Ok(hall);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult EditHall(Guid id, HallDto dto)
        {
            var hall = context.Halls.Find(id);
            if (hall == null) return NotFound();

            var other = context.Halls.FirstOrDefault(h => h.Number == dto.Number && h.HallId != id);
            if (other != null)
            {
                ModelState.AddModelError("Number", "Hall number already exists.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            hall.Number = dto.Number;
            hall.LayoutType = dto.LayoutType;

            context.SaveChanges();

            return Ok(hall);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult DeleteHall(Guid id)
        {
            var hall = context.Halls.Find(id);
            if (hall == null) return NotFound();

            context.Halls.Remove(hall);
            context.SaveChanges();

            return Ok(hall);
        }
    }
}