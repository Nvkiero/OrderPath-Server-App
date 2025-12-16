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

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ProductDetail> ListProduct { get; set; } = null!;

        public DbSet<CartItem> CartItems { get; set; } = null!;

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

    }
}
