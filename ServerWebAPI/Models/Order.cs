using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? ShipperId { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ShipperId")]
        public Shipper? Shipper { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}