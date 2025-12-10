using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;

namespace FusionComms.DTOs
{
    public class SendSMSDto
    {
        [Required]
        [StringLength(maximumLength: 15, MinimumLength = 9)]
        public string Receiver { get; set; }

        [Required]
        public string Message { get; set; }
    }

    public class CreateMailJetAttachmentDto
    {
        public string FileName { get; set; }
        public IFormFile FileToUpload { get; set; }
    }

    public class SendEmailViaMailJetDto
    {
        public string Message { get; set; }
        public string Subject { get; set; }
        public string ReciepeintAddress  { get; set; }
        public bool IsHtml { get; set; } = false;
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
    }

    public class SendTemplateEmail
    {
        public string Subject { get; set; }
        public string ReciepeintAddress  { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; } = "";
        public string SenderName { get; set; }
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
    }

    public class DynamicMailTemplate : SendTemplateEmail
    {
        public string BookingId { get; set; }
        public string CustomerFirstName { get; set; }
        public string DeliveryCode { get; set; }
        public string BookingTrackingCode { get; set; }
        public ExpandoObject Variable { get; set; }
        //public JObject Variable { get; set; }
        //public Dictionary<string, object> Variable { get; set; }
    }

    public class SendWithCustomMessage
    {
        public string Subject { get; set; }
        public string ReciepeintAddress  { get; set; }
        public string Message { get; set; }
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
    }

    public class SendEmailViaMailJetWithAttachmentsDto
    {
        public string Message { get; set; }
        public string Subject { get; set; }
        public string ReciepeintAddress  { get; set; }
        public bool IsHtml { get; set; } = false;
        public string Cc { get; set; }

        public List<IFormFile> Files { get; set; }
    }

    public class MailjetUserDetails
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class MailJetAttachment
    {
        public string ContentType { get; set; }
        public string Filename { get; set; }
        public string Base64Content { get; set; }
    }

    public class MailJetCustomProp
    {
        public MailjetUserDetails From { get; set; }
        public string Subject { get; set; }
        public string TextPart { get; set; }
        public string HTMLPart { get; set; }
        public List<MailjetUserDetails> To { get; set; }
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
        public List<MailJetAttachment> Attachments { get; set; }

        public MailJetCustomProp()
        {
            To = new List<MailjetUserDetails>();
        }
    }

    public class MailJetSenderClass
    {
        public List<MailJetCustomProp> Messages { get; set; }
    }

    public class MailJetDynamicWithTemplate
    {
        public MailjetUserDetails From { get; set; }
        public string Subject { get; set; }
        public List<MailjetUserDetails> To { get; set; }
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
        public int TemplateId { get; set; }
        public bool TemplateLanguage { get; set; }
        public object Variables { get; set; } 
    }

    public class MailJetWithTemplate<T> where T : BaseMailjetVariable
    {
        public MailjetUserDetails From { get; set; }
        public string Subject { get; set; }
        public List<MailjetUserDetails> To { get; set; }
        public List<MailjetUserDetails> Cc { get; set; }
        public List<MailjetUserDetails> Bcc { get; set; }
        public int TemplateId { get; set; }
        public bool TemplateLanguage { get; set; }
        public T Variables { get; set; }
    }
    public class MailjetOtpTemplate : BaseMailjetVariable
    {
        public string Code { get; set; }
    }

    public class BaseMailjetVariable
    {
        public string UserName { get; set; } = "User";
        public string CustomMessage { get; set; } = "";
    }

    public class CustomerMailjetVariable : BaseMailjetVariable
    {
        public string BookingId { get; set; }
        public string CustomerFirstName { get; set; }
        public string DeliveryCode { get; set; }
        public string BookingTrackingCode { get; set; }
    }

    public class SendEmailViaZepto
    {
        public ZeptoEmailAddress From { get; set; }
        public List<ZeptoReciepient> To { get; set; }
        public List<ZeptoReciepient> Cc { get; set; }
        public List<ZeptoReciepient> Bcc { get; set; }
        public bool Track_clicks { get; set; } = true;
        public bool Track_opens { get; set; } = true;
        public string Subject { get; set; }
        public string Htmlbody { get; set; }
    }
    
    public class ZeptoEmailAddress
    {
        public string Address { get; set; }
        public string Name { get; set; }
    }

    public class ZeptoSender
    {
        public string Address { get; set; }
        public string Name { get; set; }
    }
    
    public class ZeptoReciepient
    {
        public ZeptoEmailAddress Email_address { get; set; }
    }

    
    public class ZeptoMailResponseData
    {
        public string Code { get; set; }
        public List<object> AdditionalInfo { get; set; }
        public string Message { get; set; }
    }

    public class ZeptoMailResponse
    {
        public List<ZeptoMailResponseData> Data { get; set; }
        public Error Error { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
        public string Object { get; set; }
    }

    public class Detail
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public InnerError InnerError { get; set; }
        public string Target { get; set; }
    }

    public class Error
    {
        public string Code { get; set; }
        public List<Detail> Details { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
    }

    public class InnerError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    //public class MailJetVariables : BaseMailjetVariable
    //{
    //    public string UserName { get; set; }
    //}
}