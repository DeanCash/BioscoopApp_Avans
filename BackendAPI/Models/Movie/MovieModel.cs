using BackendAPI.Models.Screening;

using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Movie
{
    public class MovieModel
    {
        [Key]
        public Guid MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public int Age { get; set; }
        public string Genre { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }

        // Navigation
        public ICollection<ScreeningModel> Screenings { get; set; } = new List<ScreeningModel>();
    }
}
