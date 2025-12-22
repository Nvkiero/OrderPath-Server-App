using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;

namespace ServerWebAPI.Controllers
{
    [Route("shipper")]
    [ApiController]
    public class ApiShipper : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiShipper(AppDbContext context)
        {
            _context = context;
        }

        // GET: shipper/orders
        // Logic cũ: Lấy List Products. Logic mới: Lấy List Orders cần giao.
        [HttpGet("orders")]
        public async Task<IActionResult> GetShipperOrder()
        {
            // Lấy các đơn hàng đang ở trạng thái "Shipping" và chưa có Shipper
            var orders = await _context.Orders
                .Where(o => o.Status == "Shipping" && o.ShipperId == null)
                .Include(o => o.User)
                .Select(o => new
                {
                    Id = o.Id, // Order ID
                    Name = "Đơn hàng của " + o.User.Fullname, // Fake tên product bằng tên đơn
                    Price = o.TotalAmount, // Tổng tiền
                    Address = o.User.Address,
                    Date = o.CreatedAt
                })
                .ToListAsync();

            if (!orders.Any())
            {
                return NotFound("Không có đơn hàng nào cần vận chuyển");
            }

            return Ok(orders);
        }

        // GET: shipper/users/{id}
        // Lấy thông tin Shipper Profile
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetShipperInfo(int id)
        {
            var shipper = await _context.Shippers.FindAsync(id);

            if (shipper == null)
            {
                return NotFound(new { message = $"Shipper hiện tại không tồn tại" });
            }

            return Ok(new
            {
                shipper.Id,
                shipper.Vehicle,
                shipper.Status,
                shipper.Rating
            });
        }

        // Thêm API nhận đơn để Shipper hoạt động được với luồng mới
        [HttpPut("orders/{orderId}/accept")]
        public async Task<IActionResult> AcceptOrder(int orderId, [FromQuery] int shipperId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.ShipperId = shipperId;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã nhận đơn" });
        }
    }
}