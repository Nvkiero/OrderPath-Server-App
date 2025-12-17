using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models.Customer.Order
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double TotalAmount { get; set; }
        public int? ShipperId { get; set; }

        public string Status { get; set; } = "Pending";

        public List<OrderItem> Items { get; set; } = new();
    }
}
