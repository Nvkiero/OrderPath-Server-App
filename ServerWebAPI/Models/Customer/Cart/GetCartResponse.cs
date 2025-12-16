using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerWebAPI.Models.Customer.Product;
namespace ServerWebAPI.Models.Customer.Cart
{
   public class GetCartResponse
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
