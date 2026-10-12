namespace BP.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string PrimaryBusinessNumber { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zipcode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Title { get; set; } = "TBH";
        public string AccountType { get; set; } = string.Empty;
        public string Role { get; set; } = "billing";
        public string? ResetToken { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
    }
}
