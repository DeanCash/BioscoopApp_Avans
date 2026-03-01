

using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Screening;
using BackendAPI.Models.Seat;

namespace BackendAPI.Models.Hall
{
    public class HallModel
    {
        [Key]
        public Guid HallId { get; set; }
        public int Number { get; set; }
        public LayoutType LayoutType { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }

        // Navigation
        public ICollection<SeatModel> Seats { get; set; } = new List<SeatModel>();
        public ICollection<ScreeningModel> Screenings { get; set; } = new List<ScreeningModel>();
    }
}
