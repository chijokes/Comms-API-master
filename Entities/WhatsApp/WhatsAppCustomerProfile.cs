using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusionComms.Entities.WhatsApp
{
    [Table("WhatsAppCustomerProfiles")]
    public class CustomerProfile
    {
        [Key]
        public string ProfileId { get; set; } = Guid.NewGuid().ToString();
        public string BusinessId { get; set; }
        public string PhoneNumber { get; set; }
        public string ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BusinessId")]
        public virtual WhatsAppBusiness Business { get; set; }
        
        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    }

    [Table("WhatsAppDeliveryAddresses")]
    public class CustomerAddress
    {
        [Key]
        public string AddressId { get; set; } = Guid.NewGuid().ToString();
        public string ProfileId { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProfileId")]
        public virtual CustomerProfile CustomerProfile { get; set; }
    }
}
