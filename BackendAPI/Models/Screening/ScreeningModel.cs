using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;

namespace BackendAPI.Models.Screening
{
    public class ScreeningModel
    {
        [Key]
        public Guid ScreeningId { get; set; }      // PK

        public Guid MovieId { get; set; }          // FK
        public Guid HallId { get; set; }           // FK

        public DateTimeOffset StartTimeUtc { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }

        // Navigation
        public MovieModel Movie{ get; set; } = null!;
        public HallModel Hall { get; set; } = null!;
    }
}
