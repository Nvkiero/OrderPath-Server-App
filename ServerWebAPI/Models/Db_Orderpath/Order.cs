using ServerWebAPI.Models.Customer.Order;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Modules.Db_Orderpath
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? ShipperId { get; set; } // Có thể Null (NULL trong SQL)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ShipperId")]
        public Shipper? Shipper { get; set; }

        // Navigation property để lấy danh sách món ăn
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}