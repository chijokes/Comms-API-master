using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusionComms.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string OrganizationName { get; set; }

        [NotMapped]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string RoleName { get; set; }

        [Required]
        public bool IsAResller { get; set; } = false;
    }


    public class MontyCallBack
    {
        public string MessageId { get; set; }
        public string MobileNo { get; set; }
        public string Status { get; set; }

        public string ReceiveDate { get; set; }
        public string SendDate { get; set; }
        public decimal Rate { get; set; }
        public string StatusId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Errors { get; set; }
    }
    public class AddSesUser
    {
        public string SenderName { get; set; }

        public string SenderEmail { get; set; }
    }

    public class AddMontyUser
    {
        public string UserId { get; set; }
        public string MontyAuthCode { get; set; }
        public string MontyUserName { get; set; }
        public string MontyAPIId { get; set; }
        public string MontyPassword { get; set; }
    }
}
