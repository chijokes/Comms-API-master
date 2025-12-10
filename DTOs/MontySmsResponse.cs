namespace FusionComms.DTOs
{
    public class MontySmsResponse
    {
        public int ErrorCode { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public string OriginatingAddress { get; set; }
        public string DestinationAddress { get; set; }
        public int MessageCount { get; set; }
    }
}