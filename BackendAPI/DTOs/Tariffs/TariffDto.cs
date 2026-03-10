using BackendAPI.Models.Screening;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.DTOs.Tariffs
{
    public class TariffDto
    {
        public Guid TariffId { get; set; }         // PK
        [Required(ErrorMessage = "TariffType in minutes is required.")]
        public string TariffType { get; set; } = null!;   // unique
        [Required(ErrorMessage = "DisplayName in minutes is required.")]
        public string DisplayName { get; set; } = null!;
        public int SortOrder { get; set; }
    }
}
