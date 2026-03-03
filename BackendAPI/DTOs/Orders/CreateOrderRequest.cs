namespace BackendAPI.DTOs.Orders
{
    public sealed class CreateOrderRequest
    {
        public Guid ScreeningId { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
