using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    public class FindUserIdRequest
    {
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;
    }
    public class VerifyCodeRequest
    {
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
