using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FusionComms.DTOs
{
    public class SendSMSToMultipleDto
    {
        [Required]
        public List<string> Receiver { get; set; }

        [Required]
        public string Message { get; set; }
    }
}