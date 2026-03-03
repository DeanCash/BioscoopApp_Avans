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
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<ReservationSeatDto> Seats { get; set; } = new();
    }
}
