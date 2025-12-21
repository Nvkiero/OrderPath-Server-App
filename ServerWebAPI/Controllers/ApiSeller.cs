using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase; // Namespace chứa class SellerDB
using ServerWebAPI.Models.Seller;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServerWebAPI.Controllers
{
    [ApiController]
    [Route("seller")] // Client gọi vào: https://domain/seller/...
    public class ApiSellerController : ControllerBase
    {
        // Sửa kiểu dữ liệu từ AppDbContext sang SellerDB
        private readonly SellerDB _context;

        // Inject SellerDB vào Constructor
        public ApiSellerController(SellerDB context)
        {
            _context = context;
        }

        // ============================================
        // 1. QUẢN LÝ SẢN PHẨM (Product)
        // ============================================

        // Thêm sản phẩm
        // POST: /seller/products
        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] AddProductDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ");

            var newProduct = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Quantity = dto.Quantity,
                Image = dto.Image,
                Category = dto.Category,
                Description = dto.Description,
                Status = true // Mặc định là hiện
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            return Ok(new { ProductId = newProduct.Id, Status = "Success", Error = false });
        }

        // Sửa sản phẩm
        // PUT: /seller/products/{id}
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ChangeProductDTO dto)
        {
            if (id != dto.id) return BadRequest("ID không khớp");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { Status = "Failed", Msg = "Không tìm thấy sản phẩm" });

            // Cập nhật thông tin
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Quantity = dto.Quantity;
            product.Image = dto.Image;
            product.Category = dto.Category;
            product.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(new { Status = "Success", Error = false });
        }

        // Xóa sản phẩm
        // PUT: /seller/products/{id}/delete
        [HttpPut("products/{id}/delete")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Xóa thật khỏi database
            _context.Products.Remove(product);

            // Hoặc xóa mềm (ẩn đi) thì dùng dòng dưới:
            // product.Status = false; 

            await _context.SaveChangesAsync();
            return Ok(new { Status = "Success", Error = false });
        }

        // Lấy danh sách sản phẩm
        // GET: /seller/products
        [HttpGet("products")]
        public async Task<IActionResult> GetProductList()
        {
            var products = await _context.Products
                .Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Image = p.Image,
                    Status = p.Status
                })
                .ToListAsync();

            return Ok(products);
        }

        // ============================================
        // 2. QUẢN LÝ ĐƠN HÀNG (Order)
        // ============================================

        // Lấy danh sách đơn hàng
        // GET: /seller/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Select(o => new OrderResponseDTO
                {
                    OrderId = o.OrderId,
                    CustomerId = o.CustomerId,
                    Total = o.Total,
                    Status = o.Status,
                    Date = o.Date
                })
                .OrderByDescending(o => o.Date) // Mới nhất lên đầu
                .ToListAsync();

            return Ok(orders);
        }

        // Xác nhận đơn hàng
        // PUT: /seller/orders/{id}/confirm
        [HttpPut("orders/{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "CONFIRMED";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Status = "Confirmed",
                Error = false,
                ShipperOrderId = new Random().Next(10000, 99999) // Tạo mã vận đơn giả
            });
        }

        // Hủy đơn hàng
        // PUT: /seller/orders/{id}/cancel
        [HttpPut("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "CANCELLED";
            await _context.SaveChangesAsync();

            return Ok(new { Status = "Cancelled", Error = false });
        }
    }
}