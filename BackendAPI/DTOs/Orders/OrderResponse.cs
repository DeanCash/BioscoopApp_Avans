namespace BackendAPI.DTOs.Orders
{
    public sealed class OrderResponse
    {
        public Guid OrderId { get; init; }
        public string Status { get; init; } = default!;
        public string PaymentStatus { get; init; } = default!;
        public string PaymentMethod { get; init; } = "Pin";
        public decimal TotalAmount { get; init; }
        public string PrintCode { get; init; } = default!;
    }
}
