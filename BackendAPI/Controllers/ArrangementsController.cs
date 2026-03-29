using API.Services;
using BackendAPI.DTOs.Arrangements;
using BackendAPI.Models.Arrangement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArrangementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArrangementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetArrangements([FromQuery] ArrangementCategory? category = null)
        {
            var query = _context.Arrangements.AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(a => a.Category == category.Value);
            }

            var arrangements = await query
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.SortOrder)
                .Select(a => new
                {
                    a.ArrangementId,
                    a.Name,
                    a.Description,
                    Category = a.Category.ToString(),
                    a.Price,
                    a.SortOrder,
                    a.IsActive
                })
                .ToListAsync();

            return Ok(arrangements);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetAllArrangements()
        {
            var arrangements = await _context.Arrangements
                .OrderBy(a => a.Category)
                .ThenBy(a => a.SortOrder)
                .Select(a => new
                {
                    a.ArrangementId,
                    a.Name,
                    a.Description,
                    Category = a.Category.ToString(),
                    a.Price,
                    a.SortOrder,
                    a.IsActive,
                    a.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(arrangements);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetArrangement(Guid id)
        {
            var arrangement = await _context.Arrangements.FindAsync(id);
            if (arrangement == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                arrangement.ArrangementId,
                arrangement.Name,
                arrangement.Description,
                Category = arrangement.Category.ToString(),
                arrangement.Price,
                arrangement.SortOrder,
                arrangement.IsActive
            });
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateArrangement(ArrangementDto dto)
        {
            var existingArrangement = await _context.Arrangements
                .FirstOrDefaultAsync(a => a.Name == dto.Name);

            if (existingArrangement != null)
            {
                ModelState.AddModelError("Name", "Een arrangement met deze naam bestaat al.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var arrangement = new ArrangementModel
            {
                ArrangementId = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                Price = dto.Price,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _context.Arrangements.Add(arrangement);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                arrangement.ArrangementId,
                arrangement.Name,
                arrangement.Description,
                Category = arrangement.Category.ToString(),
                arrangement.Price,
                arrangement.SortOrder,
                arrangement.IsActive
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> EditArrangement(Guid id, ArrangementDto dto)
        {
            var arrangement = await _context.Arrangements.FindAsync(id);
            if (arrangement == null)
            {
                return NotFound();
            }

            var existingArrangement = await _context.Arrangements
                .FirstOrDefaultAsync(a => a.Name == dto.Name && a.ArrangementId != id);

            if (existingArrangement != null)
            {
                ModelState.AddModelError("Name", "Een arrangement met deze naam bestaat al.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            arrangement.Name = dto.Name;
            arrangement.Description = dto.Description;
            arrangement.Category = dto.Category;
            arrangement.Price = dto.Price;
            arrangement.SortOrder = dto.SortOrder;
            arrangement.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                arrangement.ArrangementId,
                arrangement.Name,
                arrangement.Description,
                Category = arrangement.Category.ToString(),
                arrangement.Price,
                arrangement.SortOrder,
                arrangement.IsActive
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteArrangement(Guid id)
        {
            var arrangement = await _context.Arrangements.FindAsync(id);
            if (arrangement == null)
            {
                return NotFound();
            }

            // Check of er orders zijn met dit arrangement
            var hasOrders = await _context.OrderArrangements
                .AnyAsync(oa => oa.ArrangementId == id);

            if (hasOrders)
            {
                // Soft delete: alleen deactiveren
                arrangement.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Arrangement gedeactiveerd (er zijn bestaande bestellingen)." });
            }

            _context.Arrangements.Remove(arrangement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arrangement verwijderd." });
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public IActionResult GetCategories()
        {
            var categories = Enum.GetValues<ArrangementCategory>()
                .Select(c => new
                {
                    Value = (int)c,
                    Name = c.ToString()
                })
                .ToList();

            return Ok(categories);
        }
    }
}