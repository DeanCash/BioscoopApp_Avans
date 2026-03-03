using API.Services;
using BackendAPI.DTOs.Orders;
using BackendAPI.Models.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // POST api/orders
        // Betaalmethode is altijd PIN – contant geld wordt niet geaccepteerd.
        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder(
            CreateOrderRequest request,
            CancellationToken ct = default)
        {
            var screeningExists = await _db.Screenings
                .AnyAsync(s => s.ScreeningId == request.ScreeningId, ct);

            if (!screeningExists)
                return BadRequest("Voorstelling niet gevonden.");

            var order = new OrderModel
            {
                OrderId      = Guid.NewGuid(),
                ScreeningId  = request.ScreeningId,
                SeatId       = null,
                Status        = "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = "Pin",          // Altijd PIN, nooit contant
                TotalAmount   = request.TotalAmount,
                PrintCode     = GeneratePrintCode(),
                CreatedAtUtc  = DateTimeOffset.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            return Created($"/api/orders/{order.OrderId}", ToResponse(order));
        }

        // POST api/orders/{id}/pay
        // Bevestigt de PIN-betaling en markeert de bestelling als betaald.
        [HttpPost("{id}/pay")]
        public async Task<ActionResult<OrderResponse>> ConfirmPayment(
            Guid id,
            CancellationToken ct = default)
        {
            var order = await _db.Orders.FindAsync(new object[] { id }, ct);
            if (order == null)
                return NotFound();

            order.PaymentStatus = "Paid";
            order.PaidAtUtc     = DateTimeOffset.UtcNow;
            order.Status        = "Confirmed";

            await _db.SaveChangesAsync(ct);

            return Ok(ToResponse(order));
        }

        private static OrderResponse ToResponse(OrderModel o) => new()
        {
            OrderId       = o.OrderId,
            Status        = o.Status,
            PaymentStatus = o.PaymentStatus,
            PaymentMethod = o.PaymentMethod,
            TotalAmount   = o.TotalAmount,
            PrintCode     = o.PrintCode
        };

        private static string GeneratePrintCode() =>
            Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}
