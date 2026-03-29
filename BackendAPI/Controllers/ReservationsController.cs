using BackendAPI.Models.Reservation;
using BackendAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ReservationService _reservationService;

        public ReservationsController(ReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpPost]
        public async Task<IActionResult> Reserve(ReservationRequestDto request)
        {
            try
            {
                var result = await _reservationService.ReserveAsync(
                    request.ScreeningId, request.Tickets, request.Arrangements);

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
            catch (InvalidOperationException ex) when (ex.Message == "INVALID_ARRANGEMENT")
            {
                return BadRequest("One or more arrangements are invalid or inactive");
            }
        }
    }
}
