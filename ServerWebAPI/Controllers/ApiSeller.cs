using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System.Security.Claims;

namespace ServerWebAPI.Controllers
{
    [Route("seller")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class ApiSeller : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiSeller(AppDbContext context)
        {
            _context = context;
        }

        // HELPER: Lấy thông tin Shop của User đang đăng nhập
        private async Task<Shop?> GetMyShop()
        {
            // Lấy userId từ Token
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null) return null;

            int userId = int.Parse(userIdClaim.Value);

            // Tìm Shop sở hữu bởi userId này
            return await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        }


        //1. Quản lý sản phẩm
        // Lấy danh sách sản phẩm của shop bản thân
        [HttpGet("products")]
        public async Task<IActionResult> GetMyProducts()
        {
            var shop = await GetMyShop();
            if (shop == null) return BadRequest(new { message = "Bạn chưa đăng ký Shop!" });

            var products = await _context.Products
                .Where(p => p.ShopId == shop.Id) // Chỉ lấy hàng của shop bản thân
                .Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Price = p.Price,
                    Quantity = p.Quantity ?? 0,
                    Image = p.Image ?? "",
                    Status = (p.Quantity > 0) // True nếu còn hàng
                })
                .ToListAsync();

            return Ok(products);
        }

        // Thêm sản phẩm mới
        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] AddProductDTO req)
        {
            var shop = await GetMyShop();
            if (shop == null) return BadRequest(new { message = "Bạn chưa đăng ký Shop!" });

            var newProduct = new Product
            {
                ShopId = shop.Id, // Gán sản phẩm vào shop của user đang login
                Name = req.Name,
                Price = req.Price,
                Quantity = req.Quantity,
                Image = req.Image,
                Category = req.Category,
                Description = req.Description
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm sản phẩm thành công", productId = newProduct.Id });
        }

        // Sửa sản phẩm 
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ChangeProductDTO req)
        {
            var shop = await GetMyShop();
            if (shop == null) return BadRequest(new { message = "Lỗi xác thực Shop" });

            // Tìm sản phẩm có ID trùng khớp và phải thuộc Shop của bản thân
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.ShopId == shop.Id);

            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm hoặc sản phẩm không thuộc shop của bạn" });

            // Cập nhật thông tin
            product.Name = req.Name;
            product.Price = req.Price;
            product.Quantity = req.Quantity;
            product.Image = req.Image;
            product.Category = req.Category;
            product.Description = req.Description;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công" });
        }

        // Xóa sản phẩm
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var shop = await GetMyShop();
            if (shop == null) return BadRequest(new { message = "Lỗi xác thực Shop" });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.ShopId == shop.Id);

            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm" });
        }



        //2. Quản lý đơn hàng
        // Xem danh sách đơn hàng có chứa sản phẩm của Shop mình
        [HttpGet("orders")]
        public async Task<IActionResult> GetMyShopOrders()
        {
            var shop = await GetMyShop();
            if (shop == null) return BadRequest(new { message = "Bạn chưa có Shop" });

            var orders = await _context.OrderItems
                .Where(oi => oi.ShopId == shop.Id)
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .GroupBy(oi => oi.Order) 
                .Select(g => new SellerOrderResponseDTO
                {
                    OrderId = g.Key.Id,
                    CustomerName = g.Key.User.Fullname ?? "Khách lẻ",
                    TotalRevenue = g.Sum(oi => oi.Price * (oi.Quantity ?? 0)),
                    Status = g.Key.Status ?? "Pending",
                    Date = g.Key.CreatedAt
                })
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            return Ok(orders);
        }
    }
}