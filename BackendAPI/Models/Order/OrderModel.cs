using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Seat;
using BackendAPI.Models.Screening;

namespace BackendAPI.Models.Order
{
    public class OrderModel
    {
        [Key]
        public Guid OrderId { get; set; }          // PK

        public Guid ScreeningId { get; set; }      // FK
        public Guid SeatId { get; set; }           // FK

        public string Status { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public decimal TotalAmount { get; set; }

        public string PrintCode { get; set; } = null!;    // unique

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? PaidAtUtc { get; set; }
        public DateTimeOffset? PrintedAtUtc { get; set; }

        // Navigation
        public ScreeningModel Screening { get; set; } = null!;
        public SeatModel Seat { get; set; } = null!;
    }
}