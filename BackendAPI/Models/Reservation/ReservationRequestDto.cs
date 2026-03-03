using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Reservation
{
    public class ReservationRequestDto
    {
        [Required]
        public Guid ScreeningId { get; set; }

        [Range(1, 10)]
        public int NumberOfSeats { get; set; } = 1;
    }
}
