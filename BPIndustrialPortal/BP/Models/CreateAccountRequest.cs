using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BP.Models
{
    public class CreateAccountRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        public string PrimaryBusinessNumber { get; set; } = string.Empty;

        public string AccountId { get; set; } = string.Empty;

        public string Address1 { get; set; } = string.Empty;

        public string Address2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Zipcode { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        public bool IsBroker { get; set; }

        public IEnumerable<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (IsBroker)
            {
                if (string.IsNullOrWhiteSpace(OrganizationName))
                    errors.Add("OrganizationName is required for broker accounts.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(AccountId))
                    errors.Add("Account ID is required.");
                else if (!Regex.IsMatch(AccountId, @"\A(?:[0-9]{6}|[0-9]{8})\z"))
                    errors.Add("Account ID must contain exactly 6 or 8 digits.");

                if (string.IsNullOrWhiteSpace(Address1))
                    errors.Add("Address1 is required for billing accounts.");
                if (string.IsNullOrWhiteSpace(City))
                    errors.Add("City is required for billing accounts.");
                if (string.IsNullOrWhiteSpace(State))
                    errors.Add("State is required for billing accounts.");
                if (string.IsNullOrWhiteSpace(Zipcode))
                    errors.Add("Zipcode is required for billing accounts.");
                if (string.IsNullOrWhiteSpace(Country))
                    errors.Add("Country is required for billing accounts.");
            }

            return errors;
        }
    }
}
