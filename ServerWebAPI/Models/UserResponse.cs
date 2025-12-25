using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWebAPI.Models
{
    public class UserResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }

        public int ID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public DateTime Birth { get; set; }
        public string Phone { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Role { get; set; }
        public string? Avatar { get; set; }

    }
}
