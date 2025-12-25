using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System.Security.Claims;

namespace ServerWebAPI.Controllers
{
    [Route("users")]
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
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Lấy userId từ JWT claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { Message = "Unauthorized", Status = false });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { Message = "Invalid token", Status = false });

            // Lấy user từ DB
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            // Map sang DTO UserResponse
            var response = new UserResponse
            {
                Status = true,
                Message = "Success",
                ID = user.Id,
                Username = user.Username,
                FullName = user.Fullname,
                Birth = user.Birth,
                Age = user.Age,
                Email = user.Email,
                Address = user.Address,
                Role = user.Role,
                Phone = user.Phone,
                Avatar = user.AvatarUrl
            };

            return Ok(response);
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
