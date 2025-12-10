using System.Collections.Generic;

namespace FusionComms.DTOs
{
    public class AuthResponse
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public IEnumerable<string> Claims { get; set; }
    }
}