using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models.Shipper;

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

        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<object>>> GetShipperOrder()
        {
            var products = await _context.Products
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.Image,
                })
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return NotFound("Không có đơn hàng nào cần vận chuyển");
            }

            return Ok(products);
        }

        //[HttpGet("/users/{id}")]
        //public async Task<ActionResult<ShipperProfile>> GetShipperInfo(int id)
        //{
        //    var shipper = await _context.Shippers.FindAsync(id);

        //    if (shipper == null)
        //    {
        //        return NotFound(new { message = $"Shipper hiện tại không tồn tại" });
        //    }

        //    return Ok(shipper);
        //}
    }
}
