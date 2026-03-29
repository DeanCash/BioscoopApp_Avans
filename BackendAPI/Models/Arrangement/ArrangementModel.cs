using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Arrangement
{
    public class ArrangementModel
    {
        [Key]
        public Guid ArrangementId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public ArrangementCategory Category { get; set; }

        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}