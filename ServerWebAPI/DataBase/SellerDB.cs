using Microsoft.EntityFrameworkCore;
using ServerWebAPI.Models.Seller;

namespace ServerWebAPI.DataBase
{
    public class SellerDB : DbContext
    {
        public SellerDB(DbContextOptions<SellerDB> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
    }
}