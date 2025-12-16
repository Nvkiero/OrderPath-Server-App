using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWebAPI.Models.Customer.Order
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public int? ShipperId { get; set; }

        public int Quantity { get; set; }

        public double Price { get; set; }
    }

}
