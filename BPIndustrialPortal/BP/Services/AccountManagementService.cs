using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class AccountManagementService : IAccountManagementService
    {
        private readonly string _userxmlFilePath;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AccountManagementService(IWebHostEnvironment env, IUserService userService, IEmailService emailService, IConfiguration config)
        {
            _userxmlFilePath = Path.Combine(env.ContentRootPath, "Data", "users.xml");
            _userService = userService;
            _emailService = emailService;
            _config = config;
        }

        private XDocument Load()
        {
            if (!File.Exists(_userxmlFilePath))
                throw new FileNotFoundException($"Users data file not found: {_userxmlFilePath}");
            return XDocument.Load(_userxmlFilePath);
        }

        public AccountManagementSummary GetSummary()
        {
            var allUsers = Load().Root!.Elements("User").Where(u => string.Equals((string?)u.Element("Role"), "Billing", StringComparison.OrdinalIgnoreCase)).ToList();

            int totalUsers = allUsers.Count(u => !string.Equals((string?)u.Element("Status"), "Pending", StringComparison.OrdinalIgnoreCase));
            int activeUsers = allUsers.Count(u => string.Equals((string?)u.Element("Status"), "Active", StringComparison.OrdinalIgnoreCase));
            int adminUsers = allUsers.Count(u => (string?)u.Element("AccountType") is string lvl && lvl.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
            int pendingInvitations = allUsers.Count(u => string.Equals((string?)u.Element("Status"), "Pending", StringComparison.OrdinalIgnoreCase));

            return new AccountManagementSummary
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                AdminUsers = adminUsers,
                PendingInvitations = pendingInvitations
            };
        }

        public AccountUsersResponse GetAllUsers()
        {
            var users = Load().Root!.Elements("User")
                      .Select(MapUser)
             .Where(u => u.Role.Equals("Billing", StringComparison.OrdinalIgnoreCase))
             .ToList();

            return new AccountUsersResponse
            {
                TotalEntries = users.Count,
                Users = users
            };
        }

        public PendingInvitationsResponse GetPendingInvitations()
        {
            var invitations = Load().Root!.Elements("User").Where(u => string.Equals((string?)u.Element("Status"), "Pending", StringComparison.OrdinalIgnoreCase))
             .Select(e => new PendingInvitation
             {
                 InvitationId = (string?)e.Element("UserId") ?? string.Empty,
                 FirstName = (string?)e.Element("Name") ?? string.Empty,
                 LastName = string.Empty,
                 EmailId = (string?)e.Element("EmailId") ?? string.Empty,
                 AccountLevel = (string?)e.Element("AccountType") ?? string.Empty,
                 Phone = (string?)e.Element("ContactNumber") ?? string.Empty,
                 InvitedOn = string.Empty,
                 InvitedBy = string.Empty,
                 Status = (string?)e.Element("Status") ?? string.Empty
             }).ToList();

            return new PendingInvitationsResponse
            {
                TotalEntries = invitations.Count,
                Invitations = invitations
            };
        }

        public AccountManagementPageResponse GetAccountManagementPage() => new()
        {
            Summary = GetSummary(),
            Users = GetAllUsers(),
            PendingInvitations = GetPendingInvitations()
        };

        public InviteUserResponse InviteUser(InviteUserRequest request)
        {
            if (!File.Exists(_userxmlFilePath))
                throw new FileNotFoundException($"Users data file not found: {_userxmlFilePath}");

            var existingUser = _userService.GetUserByEmail(request.EmailId);
            if (existingUser is not null)
                throw new InvalidOperationException($"A user with email '{request.EmailId}' already exists.");

            string accountType = request.PortalAdministrator ? "super admin" : "admin";

            var newUser = _userService.CreateUserFromInvite(request, accountType);
            var sent = SendInvitationEmail(newUser.UserId, newUser.Name, newUser.EmailId, request.Client);

            if (sent)
            {
                return new InviteUserResponse
                {
                    Name = request.Name,
                    EmailId = request.EmailId,
                    Role = "billing",
                    Message = $"Invitation '{request.EmailId}' successfully."
                };
            }
            return new InviteUserResponse { };
        }

        public bool SendInvitationEmail(int userId, string name, string emailId, string client)
        {
            var resetToken = _userService.GeneratePasswordResetToken(userId);

            var baseUrl = _config["AppSettings:FrontendBaseUrl"];
            var resetUrl = $"{baseUrl}/auth/set-password?token={resetToken}";
            if(!string.IsNullOrWhiteSpace(client))
            {
                resetUrl += $"&client={Uri.EscapeDataString(client)}";
            }

            var subject = $@"Set Up Your {client.ToUpper()} Energy Portal Password";

            var body = $@"
            <p>Hi {name},</p>
            <p>Your {client.ToUpper()} Energy Portal account has been created. Click the link below to set your password:</p>
            <p><a href=""{resetUrl}"">Set Password</a></p>
            <p>If you did not expect this invitation, please ignore this email.</p>";

            _ = _emailService.SendAsync(emailId, subject, body);
            return true;
        }

        public DeactivateUserResponse DeactivateUser(string userId)
        {
            if (!File.Exists(_userxmlFilePath))
                throw new FileNotFoundException($"Users data file not found: {_userxmlFilePath}");

            var doc = XDocument.Load(_userxmlFilePath);

            var userElement = doc.Root!.Elements("User").FirstOrDefault(u => string.Equals((string?)u.Element("UserId"), userId, StringComparison.OrdinalIgnoreCase));

            if (userElement is null)
                throw new KeyNotFoundException($"User '{userId}' not found.");

            var currentStatus = (string?)userElement.Element("Status") ?? string.Empty;
            var newStatus = string.Equals(currentStatus, "Active", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";

            if (userElement.Element("Status") is not null)
                userElement.Element("Status")!.Value = newStatus;
            else
                userElement.Add(new XElement("Status", newStatus));

            doc.Save(_userxmlFilePath);

            var fullName = (string?)userElement.Element("Name") ?? string.Empty;
            var action = newStatus == "Active" ? "activated" : "deactivated";

            return new DeactivateUserResponse
            {
                UserId = userId,
                Name = fullName,
                Status = newStatus,
                Message = $"User '{fullName}' has been {action} successfully."
            };
        }

        public ChangePermissionResponse ChangePermission(ChangePermissionRequest request)
        {
            if (!File.Exists(_userxmlFilePath))
                throw new FileNotFoundException($"Users data file not found: {_userxmlFilePath}");

            var allowed = new[] { "ADMIN" };
            var normalised = request.AccountLevel.Trim().ToUpperInvariant();
            if (!allowed.Contains(normalised))
                throw new ArgumentException($"Invalid AccountLevel '{request.AccountLevel}'. Allowed values: ADMIN.");

            var doc = XDocument.Load(_userxmlFilePath);

            // Root IS <Users>
            var userElement = doc.Root!.Elements("User").FirstOrDefault(u =>
                  string.Equals((string?)u.Element("UserId"), request.UserId, StringComparison.OrdinalIgnoreCase));

            if (userElement is null)
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            var fullName = (string?)userElement.Element("Name") ?? string.Empty;
            var oldAccountLevel = (string?)userElement.Element("AccountType") ?? string.Empty;

            if (userElement.Element("AccountType") is not null)
                userElement.Element("AccountType")!.Value = normalised;
            else
                userElement.Add(new XElement("AccountType", normalised));

            doc.Save(_userxmlFilePath);

            return new ChangePermissionResponse
            {
                UserId = request.UserId,
                Name = fullName,
                Message = $"Permission for '{fullName}' changed from '{oldAccountLevel}' to '{normalised}' successfully."
            };
        }

        public ResetAmPasswordResponse ResetPassword(ResetAmPasswordRequest request)
        {
            if (!File.Exists(_userxmlFilePath))
                throw new FileNotFoundException($"Users data file not found: {_userxmlFilePath}");

            var doc = XDocument.Load(_userxmlFilePath);
            var userElement = doc.Root!.Elements("User").FirstOrDefault(u => string.Equals((string?)u.Element("UserId"), request.UserId, StringComparison.OrdinalIgnoreCase));

            if (userElement is null)
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            var storedPassword = (string?)userElement.Element("Password") ?? string.Empty;

            if (!storedPassword.Equals(request.OldPassword, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                throw new ArgumentException("New password must be at least 6 characters.");

            userElement.Element("Password")!.Value = request.NewPassword;
            doc.Save(_userxmlFilePath);

            var fullName = (string?)userElement.Element("Name") ?? string.Empty;

            return new ResetAmPasswordResponse
            {
                UserId = request.UserId,
                Name = fullName,
                Message = "Password has been reset successfully."
            };
        }

        private static AccountUser MapUser(XElement e) => new()
        {
            UserId = (string?)e.Element("UserId") ?? string.Empty,
            Name = (string?)e.Element("Name") ?? string.Empty,
            Title = (string?)e.Element("Title") ?? string.Empty,
            AccountType = (string?)e.Element("AccountType") ?? string.Empty,
            Role = (string?)e.Element("Role") ?? string.Empty,
            EmailId = (string?)e.Element("EmailId") ?? string.Empty,
            Phone = (string?)e.Element("ContactNumber") ?? string.Empty,
            CreatedOn = (string?)e.Element("CreatedOn") ?? string.Empty,
            LastLogin = (string?)e.Element("LastLogin") ?? string.Empty,
            LastLogout = (string?)e.Element("LastLogout") ?? string.Empty,
            Status = (string?)e.Element("Status") ?? string.Empty,
        };
    }
}
