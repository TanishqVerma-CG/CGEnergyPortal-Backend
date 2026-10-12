namespace BP.IService
{
    public interface IPricingAlertService
    {
        string ScheduleAlert(string email, string frequency);
        void SendPricingAlertEmail(string email);
    }
}
