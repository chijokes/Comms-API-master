using System;
using System.ComponentModel.DataAnnotations;

namespace FusionComms.Entities
{
    public class OTP : BaseEntity
    {
        public string Code { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public string Channel { get; set; }
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMinutes(10);

        [Required]
        public string SenderAccountId { get; set; }
        public User SenderAccount { get; set; }
    }

    public static class OTPChannels
    {
        public const string MontySms = "MontySMS";
    }
}
