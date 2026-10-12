namespace BP.Models
{
    public class PricingAlertRequest
    {
        public string Frequency { get; set; } = "Weekly";

        public string? Email { get; set; }
    }
}
