using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Reservation
{
    public class ReservationRequestDto
    {
        [Required]
        public Guid ScreeningId { get; set; }

        [Required]
        public List<TicketLineDto> Tickets { get; set; } = new();
    }

    public class TicketLineDto
    {
        [Required]
        public Guid TariffId { get; set; }

        [Range(1, 10)]
        public int Count { get; set; }
    }
}
