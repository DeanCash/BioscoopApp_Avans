using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Hall;

namespace BackendAPI.Models.Seat
{
    public class SeatModel
    {
        [Key]
        public Guid SeatId { get; set; }           // PK

        public Guid HallId { get; set; }           // FK
        public string RowLabel { get; set; } = null!;
        public int SeatNumber { get; set; }

        // Navigation
        public HallModel Hall { get; set; } = null!;
    }
}
