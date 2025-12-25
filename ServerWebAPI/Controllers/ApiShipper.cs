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
            int shipperId = GetCurrentShipperId();
            if (shipperId == 0) return Unauthorized();

            var shipperProfile = await _context.Shippers
                .Include(s => s.User) 
                .Where(s => s.Id == shipperId) 
                .Select(s => new ShipperProfileResponse
                {
                    ShipperId = s.Id,
                    Username = s.User != null ? s.User.Fullname : "Unknown",
                    Phone = s.User != null ? s.User.Phone : "",
                    Vehicle = "Yamaha",
                    // Count orders assigned to this shipper
                    TotalDeliveries = _context.Orders.Count(o => o.ShipperId == s.Id)
                })
                .FirstOrDefaultAsync();

            if (shipperProfile == null) return NotFound("Shipper profile not found.");

            return Ok(shipperProfile);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            int shipperId = GetCurrentShipperId();
            if (shipperId == 0) return Unauthorized();

            var orders = await _context.Orders
                // CHANGE HERE: Get orders assigned to ME -OR- orders with NO SHIPPER
                .Where(o => o.ShipperId == shipperId || o.ShipperId == null)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new ShipperOrderResponse
                {
                    OrderId = o.Id,
                    CustomerName = o.User != null ? o.User.Fullname : "Unknown",
                    ShippingAddress = o.User != null ? o.User.Address : "",

                    // Join all product names into one string
                    ProductName = string.Join(", ", o.OrderItems.Select(oi =>
                        (oi.Product != null ? oi.Product.Name : "Unknown") + " x" + (oi.Quantity ?? 0))),

                    Quantity = o.OrderItems.Sum(oi => oi.Quantity ?? 0),

                    // IMPORTANT: If ShipperId is null, show "Available" or the actual status
                    CurrentStatus = o.ShipperId == null ? "Available to Pick" : o.Status,
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