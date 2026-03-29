using System.ComponentModel.DataAnnotations;
using BackendAPI.Models.Order;

namespace BackendAPI.Models.Arrangement
{
    public class OrderArrangementModel
    {
        [Key]
        public Guid OrderArrangementId { get; set; }

        public Guid OrderId { get; set; }
        public Guid ArrangementId { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        // Navigation
        public OrderModel Order { get; set; } = null!;
        public ArrangementModel Arrangement { get; set; } = null!;
    }
}