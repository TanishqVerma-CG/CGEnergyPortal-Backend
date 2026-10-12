namespace BP.Models
{
    public class ContractHistoryEntry
    {
        public string EntryId { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
    }
}
