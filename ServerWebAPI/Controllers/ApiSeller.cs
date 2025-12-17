using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerWebAPI.DataBase;

namespace ServerWebAPI.Controllers
{
    [ApiController]
    [Route("users")]
    public class ApiSeller : ControllerBase
    {
        private readonly AppDbContext _context;
        public ApiSeller(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("{id}")]

        public IActionResult GetById(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound(new
                {
                    Message = "User not found",
                    Status = false
                }); 
            }
        }
}
