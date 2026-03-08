using API.Services;
using BackendAPI.DTOs.Tariffs;
using BackendAPI.Models.Tariff;
using BackendAPI.Services.Movies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TariffsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public TariffsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public List<TariffModel> GetTariffs()
        {
            return context.Tariffs.OrderByDescending(c => c.TariffType).ToList();
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetTariff(Guid id)
        {
            var movie = context.Tariffs.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            return Ok(movie);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult CreateTariff(TariffDto tariffDto)
        {
            var otherMovie = context.Tariffs.FirstOrDefault(c => c.TariffType == tariffDto.TariffType);
            if (otherMovie != null)
            {
                ModelState.AddModelError("Title", "The movie already exists in the database");
                var validation = new ValidationProblemDetails(ModelState);
                return BadRequest(validation);
            }

            var tariff = new TariffModel
            {
                TariffType = tariffDto.TariffType,
                DisplayName = tariffDto.DisplayName,
                SortOrder = tariffDto.SortOrder,
            };

            context.Tariffs.Add(tariff);
            context.SaveChanges();

            return Ok(tariff);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult EditTariff(Guid id, TariffDto tariffDto)
        {
            var otherMovie = context.Tariffs.FirstOrDefault(c => c.TariffType == tariffDto.TariffType);
            if (otherMovie != null)
            {
                ModelState.AddModelError("Title", "The movie already exists in the database");
                var validation = new ValidationProblemDetails(ModelState);
                return BadRequest(validation);
            }

            var movie = context.Tariffs.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            movie.TariffType = tariffDto.TariffType;
            movie.DisplayName = tariffDto.DisplayName;
            movie.SortOrder = tariffDto.SortOrder;

            context.SaveChanges();

            return Ok(movie);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult DeleteTariff(Guid id)
        {
            var movie = context.Tariffs.Find(id);
            if (movie == null)
            {
                return NotFound();
            }

            context.Tariffs.Remove(movie);
            context.SaveChanges();

            return Ok(movie);
        }
    }
}
