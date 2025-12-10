using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FusionComms.DTOs.WhatsApp
{
    public class WhatsAppProductDto
    {
        [Required] public string RetailerId { get; set; }
        [Required] public string Name { get; set; }
        [Required] public decimal Price { get; set; }
        [Required] public string Currency { get; set; }
        [Required] public string Description { get; set; }
        [Required] public string ImageUrl { get; set; }
        [Required] public string Category { get; set; }
        [Required] public List<string> RevenueCenterIds { get; set; }
        public decimal? SalePrice { get; set; }
        public string Condition { get; set; } = "new";
        public string Availability { get; set; } = "in stock";
        public string Status { get; set; } = "active";
        public string Subcategory { get; set; }
    }

    public class WhatsAppProductUpdateDto
    {
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; }
        public string ImageUrl { get; set; }
        public decimal? SalePrice { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public string Availability { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public List<string> RevenueCenterIds { get; set; }
    }

    public class FeaturedProductsRequestDto
    {
        [Required] public List<string> RetailerIds { get; set; }
    }
}
