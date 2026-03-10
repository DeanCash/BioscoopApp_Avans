using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models.Tariff
{
    public class TariffModel
    {
        [Key]
        public Guid TariffId { get; set; }         // PK
        public string TariffType { get; set; } = null!;   // unique
        public string DisplayName { get; set; } = null!;
        public int SortOrder { get; set; }
        public decimal Price { get; set; }
    }
}
