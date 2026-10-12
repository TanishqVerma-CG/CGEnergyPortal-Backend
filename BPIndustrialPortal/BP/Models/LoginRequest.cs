using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public bool IsBroker { get; set; }
    }
}
