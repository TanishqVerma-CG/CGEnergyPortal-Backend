using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class ForgotPasswordRequest
    {
      [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
    }
}
