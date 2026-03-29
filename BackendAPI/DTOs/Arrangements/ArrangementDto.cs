using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Arrangement;

namespace BackendAPI.DTOs.Arrangements
{
    public class ArrangementDto
    {
        public Guid ArrangementId { get; set; }

        [Required(ErrorMessage = "Naam is verplicht.")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Categorie is verplicht.")]
        public ArrangementCategory Category { get; set; }

        [Required(ErrorMessage = "Prijs is verplicht.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prijs moet groter zijn dan 0.")]
        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}