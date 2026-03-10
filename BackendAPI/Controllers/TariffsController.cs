using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TariffsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TariffsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tariffs = await _db.Tariffs
                .OrderBy(t => t.SortOrder)
                .Select(t => new
                {
                    t.TariffId,
                    t.TariffType,
                    t.DisplayName,
                    t.Price,
                    t.SortOrder
                })
                .ToListAsync();

            return Ok(tariffs);
        }
    }
}
