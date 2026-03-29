using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace BackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StripeController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StripeController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    /// <param name="Flow">"kiosk" of "website" — bepaalt de success-URL na betaling.</param>
    public record CreateCheckoutRequest(string PrintCode, Guid ScreeningId, string FrontendBaseUrl, string Flow = "kiosk");
    public record ConfirmPaymentRequest(string SessionId);

    /// <summary>
    /// Maakt een Stripe Checkout sessie aan voor een bestaande reservering.
    /// Retourneert de URL waarnaar de browser doorgestuurd moet worden.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken ct)
    {
        var orders = await _db.Orders
            .Include(o => o.Screening)
                .ThenInclude(s => s.Movie)
            .Where(o => o.PrintCode == request.PrintCode)
            .ToListAsync(ct);

        if (orders.Count == 0)
            return NotFound("Geen orders gevonden voor deze printcode.");

        var totalCents = (long)(orders.Sum(o => o.TotalAmount) * 100);
        var movieTitle = orders.First().Screening.Movie.Title;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = totalCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name    = $"Cinema: {movieTitle}",
                            Description = $"{orders.Count} ticket(s)"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode       = "payment",
            SuccessUrl = $"{request.FrontendBaseUrl}/{request.Flow}/ticket/{request.PrintCode}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl  = $"{request.FrontendBaseUrl}/kiosk/SelectTickets/{request.ScreeningId}",
            Metadata   = new Dictionary<string, string>
            {
                ["printCode"] = request.PrintCode
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return Ok(new { url = session.Url });
    }

    /// <summary>
    /// Bevestigt de betaling na terugkeer van Stripe en markeert orders als betaald.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken ct)
    {
        Session session;
        try
        {
            var service = new SessionService();
            session = await service.GetAsync(request.SessionId, cancellationToken: ct);
        }
        catch
        {
            return BadRequest("Ongeldige Stripe-sessie.");
        }

        if (session.PaymentStatus != "paid")
            return BadRequest("Betaling is nog niet voltooid.");

        var printCode = session.Metadata["printCode"];

        var orders = await _db.Orders
            .Where(o => o.PrintCode == printCode && o.PaymentStatus != "Paid")
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            order.PaymentStatus = "Paid";
            order.Status        = "Confirmed";
            order.PaidAtUtc     = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { printCode });
    }
}
