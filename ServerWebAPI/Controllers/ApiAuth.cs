using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.Controllers;
using ServerWebAPI.Models;
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

        //Đăng ký
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister user)
        {
            if (user == null)
                return BadRequest("Invalid data");

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest("Username already exists");
            

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
                //AvatarUrl = "",
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
            return Ok("Register success");
        }

        // Đăng nhập
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            if (login == null)
                return BadRequest("Invalid data");

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == login.Username &&
                u.Password == login.Password);

            if (user == null)
                return Unauthorized("Wrong username or password");

            return Ok(user);
        }

        // Thay đổi mật khẩu
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == model.Username &&
                u.Password == model.OldPassword);

            if (user == null)
                return BadRequest("Old password incorrect");

            user.Password = model.NewPassword;
            await _context.SaveChangesAsync();

            return Ok("Password changed successfully");
        }

        // Quên mật khẩu
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
                return NotFound("Email not found");

            // reset password mặc định
            user.Password = "123456";
            await _context.SaveChangesAsync();

            return Ok("Password reset to 123456");
        }

        //// Cập nhật avatar
        //[HttpPost("avatar")]
        //public async Task<IActionResult> UpdateAvatar([FromBody] Avatar model)
        //{
        //    if (model == null)
        //        return BadRequest("Invalid data");

        //    var user = await _context.Users
        //        .FirstOrDefaultAsync(u => u.Username == model.Username);

        //    if (user == null)
        //        return NotFound("User not found");

        //    user.AvatarUrl = model.AvatarUrl;
        //    await _context.SaveChangesAsync();

        //    return Ok("Avatar updated");
        //}
    }
}
