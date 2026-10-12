using BP.IService;
using Hangfire;

namespace BP.Services
{
    public class PricingAlertService : IPricingAlertService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PricingAlertService> _logger;

        public PricingAlertService(IEmailService emailService, ILogger<PricingAlertService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }


        public string ScheduleAlert(string email, string frequency)
        {
            string cron = frequency.Equals("daily", StringComparison.OrdinalIgnoreCase) ? Cron.Daily() : frequency.Equals("monthly", StringComparison.OrdinalIgnoreCase) ? Cron.Monthly(): Cron.Weekly();

            string jobId = $"pricing-alert-email-{email}";
            RecurringJob.AddOrUpdate(jobId, () => SendPricingAlertEmail(email), cron);

            return $"Pricing alert scheduled via: Email ({frequency})";
        }

        public void SendPricingAlertEmail(string email)
        {
            _logger.LogInformation("Sending pricing alert email to {Email}", email);
            _emailService.SendAsync(email, "BP Industrial Portal – Pricing Alert","<h2>Pricing Alert</h2><p>There are new pricing updates that match your preferences. Please log in to view the latest rates.</p>").GetAwaiter().GetResult();
        }
    }
}
