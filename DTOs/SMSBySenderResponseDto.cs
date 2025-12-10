using System;

namespace FusionComms.DTOs
{
    public class SMSBySenderResponseDto
    {
        public string To { get; set; }
        public string Text { get; set; }
        public string SenderName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SenderAccountId { get; set; }
    }
}