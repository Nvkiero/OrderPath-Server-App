using Microsoft.EntityFrameworkCore;
using ServerWebAPI.Models;
using ServerWebAPI.Models.Customer.Cart;
using ServerWebAPI.Models.Customer.Order;
using ServerWebAPI.Models.Customer.Product;
namespace ServerWebAPI.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //bang users
        public DbSet<User> Users { get; set; } = null!;
        //bang products
        public DbSet<ProductDetail> ListProduct { get; set; } = null!;
        // bang cart items
        public DbSet<CartItem> CartItems { get; set; } = null!;
        // bang orders, order la 1 list orderitem(don hang)
        public DbSet<Order> Orders { get; set; } = null!;
        // bang order items(bang chi tiet don hang) 
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

    }
}
