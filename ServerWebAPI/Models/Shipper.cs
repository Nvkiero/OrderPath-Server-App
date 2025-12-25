using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models
{
    public class Shipper
    {
        [Key]
        public int Id { get; set; }
        public string ?Vehicle { get; set; } = string.Empty;
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
    public class ShipperProfileResponse
    {
        public int ShipperId { get; set; }

        public string Username {  get; set; }

        public string Phone {  get; set; }
        public string Vehicle { get; set; } = string.Empty;
        public int TotalDeliveries { get; set; }
    }

    public class ShipperOrderResponse
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }
    public class UpdateStatusRequest
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}