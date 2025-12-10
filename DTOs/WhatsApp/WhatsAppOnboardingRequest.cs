using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FusionComms.DTOs.WhatsApp
{
    public class WhatsAppOnboardingRequest
    {
        [Required] public string BusinessName { get; set; }
        [Required] public string CustomChannelId { get; set; }
        [Required] public string RestaurantId { get; set; }
        [Required] public string SourceId { get; set; }
        [Required] public string PhoneNumberId { get; set; }
        [Required] public string WabaId { get; set; }
        [Required] public string BusinessId { get; set; }
        [Required] public string Code { get; set; }
        public string AppId { get; set; } = "3388381311412817";
    }

    public class WhatsAppTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public class WhatsAppCatalogCreateResponse
    {
        public string Id { get; set; }
    }

    public class WhatsAppTemplateCreateResponse
    {
        public string Id { get; set; }
    }

    public class WhatsAppCatalogListResponse
    {
        public List<WhatsAppCatalogData> Data { get; set; }
    }

    public class WhatsAppCatalogData
    {
        public string Id { get; set; }
    }
}
