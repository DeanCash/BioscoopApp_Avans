namespace BackendAPI.Models.Reservation
{
    public class ReservationResponseDto
    {
        public Guid OrderId { get; set; }
        public string MovieTitle { get; set; } = null!;
        public int HallNumber { get; set; }
        public string RowLabel { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string PrintCode { get; set; } = null!;
    }

    public class ReservationSeatDto
    {
        public Guid OrderId { get; set; }
        public string RowLabel { get; set; } = null!;
        public int SeatNumber { get; set; }
    }

    public class ReservationGroupResponseDto
    {
        public string PrintCode { get; set; } = null!;
        public string MovieTitle { get; set; } = null!;
        public int HallNumber { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal TicketAmount { get; set; }
        public decimal ArrangementAmount { get; set; }
        public List<ReservationSeatDto> Seats { get; set; } = new();
        public List<ArrangementItemDto> Arrangements { get; set; } = new();
    }

    public class ArrangementItemDto
    {
        public Guid ArrangementId { get; set; }
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
