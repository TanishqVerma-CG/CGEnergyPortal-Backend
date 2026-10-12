namespace BP.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
