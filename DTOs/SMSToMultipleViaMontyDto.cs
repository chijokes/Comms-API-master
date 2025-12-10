using System.Collections.Generic;

namespace FusionComms.DTOs
{
    public class SMSToMultipleViaMontyDto
    {
        public string Source { get; set; }
        public List<string> Destination { get; set; }
        public string Text { get; set; }
        public string MontyAuthCode { get; set; }
        public string MontyUserName { get; set; }
        public string MontyPassword { get; set; }
        public string MontyAPIId { get; set; }
        public bool ViaResellerPlatform { get; set; }
    }
}