using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusionComms.Entities
{
    public class User : IdentityUser
    {
        //[Required]
        //public string FirstName { get; set; }

        //[Required]
        //public string LastName { get; set; }

        [Required]
        public string OrganizationName { get; set; }

        [NotMapped]
        public string Password { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public string RoleName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsAResller { get; set; }
    }
}
