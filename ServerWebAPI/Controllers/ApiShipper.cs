using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;

namespace ServerWebAPI.Controllers
{
    [Route("shipper")]
    [ApiController]
    [Authorize(Roles = "Shipper")] 
    public class ShipperController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShipperController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentShipperId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "entityId");
            if (claim == null) return 0;
            return int.Parse(claim.Value);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var shipperProfile = await _context.Shippers
                .Where(s => s.UserId == id)
                .Select(s => new ShipperProfileResponse
                {
                    ShipperId = s.Id,
                    Username = s.User.Fullname ?? "Unknown",
                    Phone = s.User.Phone ?? "",
                    Vehicle = s.Vehicle,
                    TotalDeliveries = s.Orders.Count()
                })
                .FirstOrDefaultAsync();

            if (shipperProfile == null) return NotFound("Shipper profile not found for this user.");

            return Ok(shipperProfile);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            int shipperId = GetCurrentShipperId();
            if (shipperId == 0) return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.ShipperId == shipperId)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new ShipperOrderResponse
                {
                    OrderId = o.Id,
                    CustomerName = o.User != null ? o.User.Fullname : "Unknown",
                    ShippingAddress = o.User != null ? o.User.Address : "", 
                    ProductName = o.OrderItems.Any() ? o.OrderItems.First().Product.Name : "Package",
                    Quantity = o.OrderItems.Sum(oi => oi.Quantity ?? 0),
                    CurrentStatus = o.Status,
                    OrderDate = o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            int shipperId = GetCurrentShipperId();
            if (shipperId == 0) return Unauthorized();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            order.ShipperId = shipperId; 
            order.Status = request.NewStatus;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated and shipper assigned successfully" });
        }
    }
}