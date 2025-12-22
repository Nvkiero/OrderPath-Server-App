using Microsoft.EntityFrameworkCore;
// QUAN TRỌNG: Trỏ vào thư mục chứa Model mới của bạn
using ServerWebAPI.Modules.Db_Orderpath;

namespace ServerWebAPI.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo các bảng khớp với SQL Server
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!; // Đã đổi tên class thành Product cho khớp SQL
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<Shipper> Shippers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === 1. CẤU HÌNH UNIQUE (KHỚP SQL) ===

            // Users: Username UNIQUE
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Shops: UserId UNIQUE (1 User chỉ có 1 Shop)
            modelBuilder.Entity<Shop>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            // Shippers: UserId UNIQUE (1 User chỉ làm 1 Shipper)
            modelBuilder.Entity<Shipper>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            // CartItems: 1 User chỉ có 1 dòng cho 1 Product (Constraint UQ_User_Product)
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.ProductId })
                .IsUnique();

            // === 2. CẤU HÌNH QUAN HỆ (RELATIONSHIPS) ===

            // Order - OrderItems (Xóa Order xóa luôn Items)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems) // Lưu ý: Property trong Order phải là OrderItems
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Shop - User (1-1)
            modelBuilder.Entity<Shop>()
                .HasOne(s => s.Owner)
                .WithOne() // User không cần giữ list Shop
                .HasForeignKey<Shop>(s => s.UserId);

            // === 3. ĐỊNH DẠNG TIỀN TỆ (DECIMAL) ===

            modelBuilder.Entity<Product>()
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