using API.Services;
using BackendAPI.Models.Reservation;
using BackendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ReservationService _reservationService;
        private readonly ApplicationDbContext _db;

        public ReservationsController(ReservationService reservationService, ApplicationDbContext db)
        {
            _reservationService = reservationService;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Reserve(ReservationRequestDto request)
        {
            try
            {
                var result = await _reservationService.ReserveAsync(
                    request.ScreeningId, request.Tickets);

                if (result == null)
                    return NotFound("Screening not found");

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "SOLD_OUT")
            {
                return Conflict("All seats for this screening are sold out");
            }
            catch (InvalidOperationException ex) when (ex.Message == "NO_ADJACENT_SEATS")
            {
                return Conflict("Not enough adjacent seats available for this screening");
            }
        }

        // GET api/reservations/by-code/{code}
        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> GetByPrintCode(string code, CancellationToken ct = default)
        {
            var orders = await _db.Orders
                .Include(o => o.Screening)
                    .ThenInclude(s => s.Movie)
                .Include(o => o.Screening)
                    .ThenInclude(s => s.Hall)
                .Include(o => o.Seat)
                .Where(o => o.PrintCode == code.ToUpper())
                .ToListAsync(ct);

            if (orders.Count == 0)
                return NotFound("Code niet gevonden.");

            var first = orders[0];

            var result = new ReservationGroupResponseDto
            {
                PrintCode    = first.PrintCode,
                MovieTitle   = first.Screening.Movie.Title,
                HallNumber   = first.Screening.Hall.Number,
                StartTimeUtc = first.Screening.StartTimeUtc,
                Status       = first.Status,
                TotalAmount  = orders.Sum(o => o.TotalAmount),
                Seats        = orders.Select(o => new ReservationSeatDto
                {
                    OrderId    = o.OrderId,
                    RowLabel   = o.Seat?.RowLabel ?? "-",
                    SeatNumber = o.Seat?.SeatNumber ?? 0,
                }).ToList()
            };

            return Ok(result);
        }
    }
    
    
    [HttpPost("website")]
    public async Task<IActionResult> ReserveWebsite(WebsiteReservationRequestDto request)
    {
        try
        {
            var result = await _reservationService.ReserveSpecificSeatsAsync(
                request.ScreeningId, request.SeatTickets);

            if (result == null)
                return NotFound("Screening not found");

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "SEATS_TAKEN")
        {
            return Conflict("One or more selected seats are already taken");
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_SEATS")
        {
            return BadRequest("One or more seats do not belong to this hall");
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_TARIFF")
        {
            return BadRequest("Invalid tariff selected");
        }
    }
}
