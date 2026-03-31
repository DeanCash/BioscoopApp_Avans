using Stripe.Checkout;

namespace BackendAPI.Services.Stripe;

public sealed class StripeSessionFetcher : IStripeSessionFetcher
{
    public async Task<Session> GetAsync(string sessionId, CancellationToken ct = default)
    {
        var service = new SessionService();
        return await service.GetAsync(sessionId, cancellationToken: ct);
    }
}
