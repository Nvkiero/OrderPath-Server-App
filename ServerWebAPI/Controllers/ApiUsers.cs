using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System.Security.Claims;

namespace ServerWebAPI.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class ApiUsers : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiUsers(AppDbContext context)
        {
            _context = context;
        }

 
        // Helper: lấy userId từ token
        
        private int GetUserIdFromToken()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // GET: api/users/me
        [HttpGet("me")]
        public IActionResult GetMyProfile()
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            return Ok(new
            {
                Message = "Success",
                Status = true,
                Data = new
                {
                    user.Id,
                    user.Username,
                    user.Fullname,
                    user.Email,
                    user.Phone,
                    user.Address,
                    Role = "User"
                }
            });
        }

        // PUT: api/users/me
        [HttpPut("me")]
        public IActionResult UpdateProfile([FromBody] UpdateUserDTO dto)
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            if (dto.Fullname != null) user.Fullname = dto.Fullname;
            if (dto.Email != null) user.Email = dto.Email;
            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.Address != null) user.Address = dto.Address;

            _context.SaveChanges();
            return Ok(new { Message = "Update success", Status = true });
        }

        // PUT: api/users/me/change-password
        [HttpPut("me/change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            string oldHash = ApiAuth.HashPassword(dto.OldPassword);
            if (user.PasswordHash != oldHash)
            {
                return BadRequest(new { Message = "Old password incorrect", Status = false });
            }

            user.PasswordHash = ApiAuth.HashPassword(dto.NewPassword);
            _context.SaveChanges();

            return Ok(new { Message = "Password changed successfully", Status = true });
        }

        // PUT: api/users/me/avatar
        [HttpPut("me/avatar")]
        public IActionResult UploadAvatar(IFormFile avatar)
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            string avatarUrl = $"https://server/avatar/user_{userId}.jpg";

            // user.Avatar = avatarUrl;
            // _context.SaveChanges();

            return Ok(new
            {
                Message = "Upload avatar success",
                Status = true,
                Avatar = avatarUrl
            });
        }
    }
}
