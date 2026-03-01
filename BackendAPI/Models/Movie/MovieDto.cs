using BackendAPI.Models.Screening;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Movie
{
    public class MovieDto
    {
   
        public Guid MovieId { get; set; }
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = null!;
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = null!;
        [Required(ErrorMessage = "Duration in minutes is required.")]
        public int DurationMinutes { get; set; }
        [Required(ErrorMessage = "Age is required.")]
        public int Age { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }

        // Navigation
        public ICollection<ScreeningModel> Screenings { get; set; } = new List<ScreeningModel>();
    }
}
