using BP.IService;
using BP.Models;
using System;
using System.Data;
using System.Xml.Linq;

namespace BP.Services
{
    public class UserService : IUserService
    {
        private static readonly object AccountCreationLock = new();
        private readonly string _xmlFilePath;

        public UserService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "users.xml");
        }
        private IEnumerable<User> ReadUsersFromXml()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            return doc.Root!.Elements("User")
                .Select(e => new User
                {
                    UserId = (int)e.Element("UserId")!,
                    Name = (string?)e.Element("Name") ?? string.Empty,
                    EmailId = (string?)e.Element("EmailId") ?? string.Empty,
                    ContactNumber = (string?)e.Element("ContactNumber") ?? string.Empty,
                    PrimaryBusinessNumber = (string?)e.Element("PrimaryBusinessNumber") ?? string.Empty,
                    Address1 = (string?)e.Element("Address1") ?? string.Empty,
                    Address2 = (string?)e.Element("Address2") ?? string.Empty,
                    City = (string?)e.Element("City") ?? string.Empty,
                    State = (string?)e.Element("State") ?? string.Empty,
                    Zipcode = (string?)e.Element("Zipcode") ?? string.Empty,
                    Country = (string?)e.Element("Country") ?? string.Empty,
                    Password = (string?)e.Element("Password") ?? string.Empty,
                    Title = (string?)e.Element("Title") ?? "TBH",
                    AccountType = (string?)e.Element("AccountType") ?? string.Empty,
                    Role = (string?)e.Element("Role") ?? "billing",
                    OrganizationName = (string?)e.Element("OrganizationName") ?? string.Empty,
                })
                .ToList();
        }

        public User? ValidateUser(string emailId, string password, string role)
        {
            return ReadUsersFromXml().FirstOrDefault(u => u.EmailId.Equals(emailId, StringComparison.OrdinalIgnoreCase) && u.Password == password && u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }
        public User? GetUserById(int userId)
        {
            return ReadUsersFromXml().FirstOrDefault(u => u.UserId == userId);
        }

        public User? GetUserByEmail(string emailId)
        {
            return ReadUsersFromXml().FirstOrDefault(u => u.EmailId.Equals(emailId, StringComparison.OrdinalIgnoreCase));
        }

        public UpdateUser? UpdateUserDetail(UpdateUser user)
        {
            try
            {
                if (!File.Exists(_xmlFilePath))
                    throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

                var doc = XDocument.Load(_xmlFilePath);

                var userElement = doc.Root!.Elements("User").FirstOrDefault(e => (int)e.Element("UserId")! == user.UserId);

                if (userElement is null)
                    return null;

                userElement.Element("Name")!.Value = user.Name;
                userElement.Element("EmailId")!.Value = user.EmailId;
                userElement.Element("ContactNumber")!.Value = user.ContactNumber;
                userElement.Element("PrimaryBusinessNumber")!.Value = user.PrimaryBusinessNumber;

                var storedRole = (string?)userElement.Element("Role") ?? string.Empty;

                if (string.Equals(storedRole, "Broker", StringComparison.OrdinalIgnoreCase))
                {
                    var orgEl = userElement.Element("OrganizationName");
                    if (orgEl is null)
                        userElement.Add(new XElement("OrganizationName", user.OrganizationName ?? string.Empty));
                    else
                        orgEl.Value = user.OrganizationName ?? string.Empty;
                }

                doc.Save(_xmlFilePath);

                var returnedOrg = (string?)userElement.Element("OrganizationName") ?? string.Empty;

                return new UpdateUser
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    EmailId = user.EmailId,
                    ContactNumber = user.ContactNumber,
                    PrimaryBusinessNumber = user.PrimaryBusinessNumber,
                    Role = storedRole,
                    OrganizationName = returnedOrg
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public UserAddress? UpdateUserAddress(UserAddress user)
        {
            try
            {
                if (!File.Exists(_xmlFilePath))
                    throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

                var doc = XDocument.Load(_xmlFilePath);

                var userElement = doc.Root!.Elements("User").FirstOrDefault(e => (int)e.Element("UserId")! == user.UserId);

                if (userElement is null)
                    return null;

                userElement.Element("Address1")!.Value = user.Address1;
                userElement.Element("Address2")!.Value = user.Address2;
                userElement.Element("City")!.Value = user.City;
                userElement.Element("State")!.Value = user.State;
                userElement.Element("Zipcode")!.Value = user.Zipcode;
                userElement.Element("Country")!.Value = user.Country;
                doc.Save(_xmlFilePath);

                return new UserAddress
                {
                    UserId = user.UserId,
                    Address1 = user.Address1,
                    Address2 = user.Address2,
                    City = user.City,
                    State = user.State,
                    Zipcode = user.Zipcode,
                    Country = user.Country
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool ChangePassword(ChangePassword request)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User").FirstOrDefault(e => (int)e.Element("UserId")! == request.UserId);

            if (userElement is null)
                return false;

            var storedPassword = (string?)userElement.Element("Password") ?? string.Empty;

            if (!storedPassword.Equals(request.OldPassword))
                return false;

            userElement.Element("Password")!.Value = request.NewPassword;
            doc.Save(_xmlFilePath);

            return true;
        }

        public bool ResetPassword(int userId, string newPassword)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User").FirstOrDefault(e => (int)e.Element("UserId")! == userId);

            if (userElement is null)
                return false;

            userElement.Element("Password")!.Value = newPassword;
            doc.Save(_xmlFilePath);

            return true;
        }

        public User? CreateUser(CreateAccountRequest request)
        {
            lock (AccountCreationLock)
            {
                return CreateUserCore(request);
            }
        }

        private User? CreateUserCore(CreateAccountRequest request)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            if (!request.IsBroker && doc.Root!.Elements("User")
                .Any(element => string.Equals((string?)element.Element("AccountId"), request.AccountId, StringComparison.Ordinal)))
                throw new DuplicateNameException("Account already exists with this Account ID.");

            var existing = doc.Root!.Elements("User")
                .FirstOrDefault(e => ((string?)e.Element("EmailId") ?? string.Empty)
                    .Equals(request.EmailId, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                return null;

            var newId = doc.Root.Elements("User")
                .Max(e => (int?)e.Element("UserId") ??0) +1;

            var address1 = request.IsBroker ? string.Empty : request.Address1;
            var address2 = request.IsBroker ? string.Empty : request.Address2;
            var city = request.IsBroker ? string.Empty : request.City;
            var state = request.IsBroker ? string.Empty : request.State;
            var zipcode = request.IsBroker ? string.Empty : request.Zipcode;
            var country = request.IsBroker ? string.Empty : request.Country;
            var orgName = request.IsBroker ? request.OrganizationName : string.Empty;
            var role = request.IsBroker ? "Broker" : "billing";

            var newUser = new XElement("User",
                new XElement("UserId", newId),
                request.IsBroker ? null : new XElement("AccountId", request.AccountId),
                new XElement("Name", request.Name),
                new XElement("EmailId", request.EmailId),
                new XElement("ContactNumber", request.ContactNumber),
                new XElement("PrimaryBusinessNumber", request.PrimaryBusinessNumber),
                new XElement("Address1", address1),
                new XElement("Address2", address2),
                new XElement("City", city),
                new XElement("State", state),
                new XElement("Zipcode", zipcode),
                new XElement("Country", country),
                new XElement("Password", request.Password),
                new XElement("Title", "TBD"),
                new XElement("AccountType", "Admin"),
                new XElement("Role", role),
                new XElement("Status", "Active"),
                new XElement("CreatedOn", DateTime.Now.ToString("M/d/yyyy")),
                new XElement("LastLogin", string.Empty),
                new XElement("LastLogout", string.Empty),
                new XElement("PrimaryAccountHolder", false),
                new XElement("PortalAdministrator", false),
                new XElement("ContractRenewalContact", false),
                new XElement("BillingContact", false),
                new XElement("AccountsPayableContact", false),
                new XElement("TaxExemptionContact", false),
                new XElement("UsageReportingContact", false),
                new XElement("OrganizationName", orgName)
            );

            doc.Root.Add(newUser);
            doc.Save(_xmlFilePath);

            return new User
            {
                UserId = newId,
                Name = request.Name,
                EmailId = request.EmailId,
                ContactNumber = request.ContactNumber,
                PrimaryBusinessNumber = request.PrimaryBusinessNumber,
                Address1 = address1,
                Address2 = address2,
                City = city,
                State = state,
                Zipcode = zipcode,
                Country = country,
                Password = request.Password,
                Role = role,
                OrganizationName = orgName
            };
        }

        public User CreateUserFromInvite(InviteUserRequest request, string accountType)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var newId = doc.Root!.Elements("User")
                .Max(e => (int?)e.Element("UserId") ??0) +1;

            var newUserEl = new XElement("User",
                new XElement("UserId", newId),
                new XElement("Name", request.Name),
                new XElement("EmailId", request.EmailId),
                new XElement("ContactNumber", request.Phone),
                new XElement("PrimaryBusinessNumber", string.Empty),
                new XElement("Address1", string.Empty),
                new XElement("Address2", string.Empty),
                new XElement("City", string.Empty),
                new XElement("State", string.Empty),
                new XElement("Zipcode", string.Empty),
                new XElement("Country", string.Empty),
                new XElement("Password", string.Empty),
                new XElement("Title", "TBD"),
                new XElement("AccountType", accountType),
                new XElement("Role", "billing"),
                new XElement("Status", "Pending"),
                new XElement("CreatedOn", DateTime.Now.ToString("M/d/yyyy")),
                new XElement("LastLogin", string.Empty),
                new XElement("LastLogout", string.Empty),
                new XElement("PrimaryAccountHolder", request.PrimaryAccountHolder),
                new XElement("PortalAdministrator", request.PortalAdministrator),
                new XElement("ContractRenewalContact", request.ContractRenewalContact),
                new XElement("BillingContact", request.BillingContact),
                new XElement("AccountsPayableContact", request.AccountsPayableContact),
                new XElement("TaxExemptionContact", request.TaxExemptionContact),
                new XElement("UsageReportingContact", request.UsageReportingContact),
                new XElement("OrganizationName", string.Empty)
            );
            
            doc.Root.Add(newUserEl);
            doc.Save(_xmlFilePath);

            return new User
            {
                UserId = newId,
                Name = request.Name,
                EmailId = request.EmailId,
                ContactNumber = request.Phone,
                AccountType = accountType,
                Role = "billing"
            };
        }

        public string GeneratePasswordResetToken(int userId)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User")
                .FirstOrDefault(e => (int?)e.Element("UserId") == userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            var token = Guid.NewGuid().ToString("N");

            var tokenElement = userElement.Element("ResetToken");
            if (tokenElement is null)
                userElement.Add(new XElement("ResetToken", token));
            else
                tokenElement.Value = token;

            doc.Save(_xmlFilePath);

            return token;
        }

        public bool ResetPasswordByToken(string token, string newPassword)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User").FirstOrDefault(e =>string.Equals((string?)e.Element("ResetToken"), token, StringComparison.Ordinal));

            if (userElement is null)
                return false;

            userElement.Element("Password")!.Value = newPassword;
            userElement.Element("Status")!.Value = "Active";
            var tokenElement = userElement.Element("ResetToken");
            tokenElement?.Remove();

            doc.Save(_xmlFilePath);

            return true;
        }

        public string GenerateVerificationCode(int userId)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User")
                .FirstOrDefault(e => (int?)e.Element("UserId") == userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            var code = Random.Shared.Next(100000, 999999).ToString();

            var codeElement = userElement.Element("VerificationCode");
            if (codeElement is null)
                userElement.Add(new XElement("VerificationCode", code));
            else
                codeElement.Value = code;

            doc.Save(_xmlFilePath);

            return code;
        }

        public bool VerifyAndConsumeVerificationCode(int userId, string code)
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"User data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);

            var userElement = doc.Root!.Elements("User")
                .FirstOrDefault(e => (int?)e.Element("UserId") == userId);

            if (userElement is null)
                return false;

            var stored = (string?)userElement.Element("VerificationCode");

            if (!string.Equals(stored, code, StringComparison.Ordinal))
                return false;

            userElement.Element("VerificationCode")?.Remove();
            doc.Save(_xmlFilePath);

            return true;
        }
    }
}
