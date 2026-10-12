namespace BP.Models
{
    public class RequestTypeItem
    {
        public string Label { get; set; } = string.Empty;
        public string ServiceRep { get; set; } = string.Empty;
    }

    public class RequestTypeListResponse
    {
        public List<RequestTypeItem> RequestTypes { get; set; } = [];
    }
}
