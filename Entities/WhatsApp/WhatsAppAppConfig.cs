using System;
using System.ComponentModel.DataAnnotations;

namespace FusionComms.Entities.WhatsApp
{
    public class WhatsAppAppConfig
    {
        [Key][MaxLength(50)] public string ConfigId { get; set; } = Guid.NewGuid().ToString();
        [Required][MaxLength(50)] public string AppId { get; set; }
        [Required][MaxLength(255)] public string AppSecret { get; set; }
        [Required][MaxLength(500)] public string AccessToken { get; set; }
        [Required][MaxLength(255)] public string VerifyToken { get; set; }
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
