using Stripe.Checkout;

namespace BackendAPI.Services.Stripe;

public interface IStripeSessionFetcher
{
    Task<Session> GetAsync(string sessionId, CancellationToken ct = default);
}
