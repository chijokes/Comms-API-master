using System;

namespace FusionComms.Entities
{
    public class EmailNotification : BaseEntity
    {
        public string Receipient { get; set; }
        public string Text { get; set; }
        public string Subject { get; set; }

        public string SenderAddress { get; set; }
        public string SenderName { get; set; }

        public string SenderAccountId { get; set; }
        public User SenderAccount { get; set; }

    }
    public class SentEmail
    {
        public int Id { get; set; }
        public string RecipientAddress { get; set; }
        public string TemplateId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }

}
