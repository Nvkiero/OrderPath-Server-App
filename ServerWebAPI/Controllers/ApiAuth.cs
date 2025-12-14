using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.Controllers;
using ServerWebAPI.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ServerWebAPI.API
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
        // Đăng kí
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister user)
        {
            if (user == null)
                return BadRequest("");
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new
            {
                message = "Đăng kí thành công",
                userId = newUser.Id
            });
        }
    }
}
