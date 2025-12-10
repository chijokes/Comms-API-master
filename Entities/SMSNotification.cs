using System.ComponentModel.DataAnnotations;

namespace FusionComms.Entities
{
    public class SMSNotification : BaseEntity
    {
        public string Receiver { get; set; }
        public string Text { get; set; }
        public string SenderName { get; set; }

        [Required]
        public string SenderAccountId { get; set; }
        public User SenderAccount { get; set; }

        public bool IsSentSuccessfully { get; set; }
        public bool IsDelivered { get; set; }

        public string MontyNotificationId { get; set; }
    }
}
