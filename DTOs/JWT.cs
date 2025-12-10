namespace FusionComms.DTOs
{
    public class JWT
    {
        public string SigningKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
