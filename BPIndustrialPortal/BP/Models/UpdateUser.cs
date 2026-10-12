using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class UpdateUser
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string EmailId { get; set; } = string.Empty;
        [Required]
        public string ContactNumber { get; set; } = string.Empty;
        public string PrimaryBusinessNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
    }

    public class UserAddress
    {
        [Required]
        public int UserId { get; set; } 
        [Required]
        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        [Required]
        public string State { get; set; } = string.Empty;
        [Required]
        public string Zipcode { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;

    }
    public class ChangePassword
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string OldPassword { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;

    }

}
