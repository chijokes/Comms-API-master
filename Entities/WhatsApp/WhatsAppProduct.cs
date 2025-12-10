using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusionComms.Entities.WhatsApp
{
    public class WhatsAppProduct
    {
        [Required] public string ProductId { get; set; }
        [Required] public string RetailerId { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Category { get; set; }
        public string Subcategory { get; set; }
        [Required] public string RevenueCenterId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string SetId { get; set; }
        [ForeignKey("SetId")] public WhatsAppProductSet ProductSet { get; set; }
        public bool IsFeatured { get; set; } = false;
        public string CompositeKey => $"{RetailerId}_{RevenueCenterId}";
    }

    public class WhatsAppProductSet
    {
        [Key] public string SetId { get; set; }
        public string CatalogId { get; set; }
        public string BusinessId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("BusinessId")] public WhatsAppBusiness Business { get; set; }
    }
}
