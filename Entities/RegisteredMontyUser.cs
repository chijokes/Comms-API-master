namespace FusionComms.Entities
{
    public class RegisteredMontyUser : BaseEntity
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public string MontyAuthCode { get; set; }
        public string MontyUserName { get; set; }
        public string MontyAPIId { get; set; }
        public string MontyPassword { get; set; }
        public string Source { get; set; }
    }
}