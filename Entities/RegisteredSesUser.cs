namespace FusionComms.Entities
{
    public class RegisteredSesUser : BaseEntity
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public string SenderName { get; set; }

        //This needs to also be set up on amazon ses platform
        // For now, we'll be sending email with Fusion's official email address
        public string SenderEmail { get; set; }
        public string MailJetOtpId { get; set; }
        
        public string PrimaryProvider { get; set; }
    }
}