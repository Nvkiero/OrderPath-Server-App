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
    // Đăng kí
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
            try
            {
                if (user == null)
                    return BadRequest("");
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    return BadRequest();


                var newUser = new User
                {
                    Username = user.Username,
                    Password = HashPassword(user.Password),
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
                return Ok();
            } catch
            {
                MessageBox.Show("BUG");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            try
            {
                if (login == null)
                    return BadRequest("Nhập thông tin đầy đủ.");

                string hashedPassword = HashPassword(login.Password);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == hashedPassword);

                if (user == null)
                    return Unauthorized("Sai tên hoặc password.");

                var token = GenerateJwtToken(user)

                return Ok(new { message = "Đăng nhập thành công", userId = user.Id, token });
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                audience: HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    double.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiresInMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
