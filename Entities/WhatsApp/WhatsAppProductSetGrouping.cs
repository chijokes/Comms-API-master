using System;
using System.ComponentModel.DataAnnotations;

namespace FusionComms.Entities.WhatsApp
{
	public class WhatsAppProductSetGrouping
	{
		[Key] public string Id { get; set; } = Guid.NewGuid().ToString();
		[Required] public string BusinessId { get; set; }
		[Required] public string GroupName { get; set; }
		[Required] public string ProductSetIds { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
	}
}


