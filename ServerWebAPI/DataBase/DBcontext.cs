using Microsoft.EntityFrameworkCore;
using ServerWebAPI.Models;
using ServerWebAPI.Models.Customer.Cart;
using ServerWebAPI.Models.Customer.Order;
using ServerWebAPI.Models.Customer.Product;
using ServerWebAPI.Models.Shop;
using ServerWebAPI.Models.Shipper;

namespace ServerWebAPI.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ProductDetail> Products { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<Shipper> Shippers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cart: 1 user + 1 product chỉ 1 dòng
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.ProductId })
                .IsUnique();

            // Order - OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne<Order>()
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal cho tiền
            modelBuilder.Entity<ProductDetail>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
