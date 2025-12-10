namespace FusionComms.DTOs
{
    public class SendEMailViaSESDto : SendEmailDto
    {
        public string SenderAddress { get; set; }
        public string SenderName { get; set; }

    }

    public class SendEmailDto 
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Recepient { get; set; }
    }
}
