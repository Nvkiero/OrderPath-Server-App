using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerWebAPI.Models.Customer.Product;
using ServerWebAPI.Modules.Db_Orderpath;
namespace ServerWebAPI.Models.Customer.Cart
{
   public class GetCartResponse
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
