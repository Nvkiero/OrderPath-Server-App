using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWebAPI.Models
{
    public class RegisterDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Birth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
