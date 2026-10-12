using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class AccountManagementSummary
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int PendingInvitations { get; set; }
    }
    public class AccountUser
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AccountLevel { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string LastLogin { get; set; } = string.Empty;
        public string LastLogout { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AccountUsersResponse
    {
        public int TotalEntries { get; set; }
        public List<AccountUser> Users { get; set; } = new List<AccountUser>();
    }
    public class PendingInvitation
    {
        public string InvitationId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name => $"{FirstName} {LastName}".Trim();
        public string EmailId { get; set; } = string.Empty;
        public string AccountLevel { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string InvitedOn { get; set; } = string.Empty;
        public string InvitedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PendingInvitationsResponse
    {
        public int TotalEntries { get; set; }
        public List<PendingInvitation> Invitations { get; set; } = [];
    }
    public class AccountManagementPageResponse
    {
        public AccountManagementSummary Summary { get; set; } = new();
        public AccountUsersResponse Users { get; set; } = new();
        public PendingInvitationsResponse PendingInvitations { get; set; } = new();
    }
    public class InviteUserRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty; 
        [Required]
        public string Phone { get; set; } = string.Empty;
        public bool PrimaryAccountHolder { get; set; }
        public bool PortalAdministrator { get; set; }
        public bool ContractRenewalContact { get; set; }
        public bool BillingContact { get; set; }
        public bool AccountsPayableContact { get; set; }
        public bool TaxExemptionContact { get; set; }
        public bool UsageReportingContact { get; set; }
        public string Client { get; set; } = string.Empty;
    }

    public class InviteUserResponse
    {
        public string Name { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    public class DeactivateUserResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ChangePermissionRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string AccountLevel { get; set; } = string.Empty;
    }

    public class ChangePermissionResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    public class ResetAmPasswordRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string OldPassword { get; set; } = string.Empty;
        [Required]
        [MinLength(6, ErrorMessage = "NewPassword must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetAmPasswordResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}