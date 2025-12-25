namespace ServerWebAPI.Models
{
    public class OtpInfo
    {
        public string Code { get; set; }
        public DateTime ExpiredAt { get; set; }
    }
}
