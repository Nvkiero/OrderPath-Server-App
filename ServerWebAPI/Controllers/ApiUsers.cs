using Microsoft.AspNetCore.Mvc;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;

namespace ServerWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ApiUsers : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiUsers(AppDbContext context)
        {
            _context = context;
        }
         // GET users/{id}

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

            return Ok(new
            {
                Message = "Success",
                Status = true,
                user.Username,
                ID = user.Id,
                FullName = user.Fullname,
                user.Birth,
                user.Age,
                user.Email,
                user.Address,
                Role = "User" // fake role, nâng cấp JWT sau
            });
        }
        // PUT users/{id}
        // Cập nhật hồ sơ cá nhân
        [HttpPut("{id}")]
        public IActionResult UpdateProfile(int id, [FromBody] UpdateUserDTO dto)
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

            user.Fullname = dto.Fullname;
            user.Birth = dto.Birth;
            user.Age = dto.Age;
            user.Email = dto.Email;
            user.Address = dto.Address;

            _context.SaveChanges();

            return Ok(new
            {
                Message = "Update success",
                Status = true,
                user.Fullname,
                user.Birth,
                user.Age,
                user.Email,
                user.Address
            });
        }

        // PUT users/{id}/change-password

        [HttpPut("{id}/change-password")]
        public IActionResult ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
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

            if (user.Password != dto.OldPassword)
            {
                return BadRequest(new
                {
                    Message = "Old password incorrect",
                    Status = false
                });
            }

            user.Password = dto.NewPassword;
            _context.SaveChanges();

            return Ok(new
            {
                Message = "Password changed successfully",
                Status = true
            });
        }

        // PUT users/{id}/avatar
        [HttpPut("{id}/avatar")]
        public IActionResult UploadAvatar(int id, IFormFile avatar)
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

            // Demo: fake URL avatar
            string avatarUrl = $"https://server/avatar/user_{id}.jpg";

            return Ok(new
            {
                Message = "Upload avatar success",
                Status = true,
                Avatar = avatarUrl
            });
        }
    }
}
