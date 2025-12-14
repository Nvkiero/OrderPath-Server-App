using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using System.Net;
using System.Numerics;
namespace ServerWebAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister user)
        {
            if (user == null)
                return BadRequest();
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest();

            var newUser = new User
            {
                Username = user.Username,
                Password = user.Password,
                Fullname = user.Fullname,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Age = user.Age,
                Birth = user.Birth,
            };

            try
{
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }
    }

}
