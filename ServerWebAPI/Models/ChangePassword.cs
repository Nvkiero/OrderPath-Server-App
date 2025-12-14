namespace ServerWebAPI.Models
{
    public class ChangePassword
    {
        public string Username { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
