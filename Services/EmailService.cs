using FusionComms.DTOs;
using FusionComms.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IEmailService
    {
        Task<bool> SendToSingle(SendEmailDto emailModel, CancellationToken token);
        Task<bool> SendViaMailJet(string recipientAddress, string message, string subject, CancellationToken token, bool isHtml = false, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null);

        Task<bool> SendOTP(string recipient, CancellationToken cancellation, int numberOfDigits);

        Task<bool> SendOTPWithGeneratedCode(string recipient, string code, CancellationToken cancellation, int numberOfDigits);

        Task<(bool status, string message)> VerifyOTP(string recipient, string otp);

        Task<bool> SendWithAttachmentsViaMailJet(string recipientAddress, string message, string subject, List<CreateMailJetAttachmentDto> attachments, CancellationToken token, string cc, bool isHtml = false);

        Task<bool> SendTransactionViaMailJet(string recipientAddress, string subject, int? templateId,
            BaseMailjetVariable variable, CancellationToken token, int numberOfDigits, string senderName = null, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null);

        Task<bool> SendDynamicEmailViaMailjet(string recipientAddress, string subject, int templateId,
            object variable, CancellationToken token, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null);
    }
    public class EmailService : IEmailService
    {
        private readonly IRepository repository;
        private readonly HttpContext httpContext;
        private readonly IAmazonSESService sesService;
        private readonly IMailJetService mailJetService;
        private readonly IZeptoMailService _zeptoMailService;
        private readonly IOTPService oTPService;
        public EmailService(IHttpContextAccessor httpContext, IAmazonSESService sesService, IRepository repository, 
            IMailJetService mailJetService, IOTPService oTPService, IZeptoMailService zeptoMailService)
        {
            this.httpContext = httpContext.HttpContext;
            this.sesService = sesService;
            this.repository = repository;
            this.mailJetService = mailJetService;
            this.oTPService = oTPService;
            _zeptoMailService = zeptoMailService;
        }


        //implement check to decide which emailing service to send to
        public async Task<bool> SendToSingle(SendEmailDto emailModel, CancellationToken token)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), token);

            if (registeredMontyUser == null)
                return false;

            var emailSendResult = await sesService.SendEMail(new SendEMailViaSESDto()
            {
                Message = emailModel.Message,
                Recepient = emailModel.Recepient,
                SenderAddress = registeredMontyUser.SenderEmail,
                SenderName = registeredMontyUser.SenderName,
                Subject = emailModel.Subject
            }, token);

            await SaveEmailNotification(new EmailNotification()
            {
                CreatedAt = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString(),
                Receipient = emailModel.Recepient,
                SenderAccountId = registeredMontyUser.UserId,
                SenderAddress = registeredMontyUser.SenderEmail,
                SenderName = registeredMontyUser.SenderName,
                Subject = emailModel.Subject,
                Text = emailModel.Message
            }, token);

            return emailSendResult; 
        }

        public async Task<bool> SendViaMailJet(string recipientAddress, string message, string subject, CancellationToken token, bool isHtml = false, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), token);

            if (registeredMontyUser == null)
                return false;

            bool sendResult = false;
            if (registeredMontyUser.PrimaryProvider is not null && registeredMontyUser.PrimaryProvider.Equals("zepto", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _zeptoMailService.SendEmail(new SendEmailViaZepto()
                {
                    From = new ZeptoEmailAddress()
                    {
                        Address = registeredMontyUser.SenderEmail,
                        Name = registeredMontyUser.SenderName
                    },
                    Subject = subject,
                    Htmlbody = message,
                    To = new List<ZeptoReciepient>()
                    {
                        new ZeptoReciepient()
                        {
                            Email_address = new ZeptoEmailAddress()
                            {
                                Address = recipientAddress
                            }
                        }
                    },
                    Bcc = Bcc is not null && Bcc.Count > 0 ? Bcc.Select(x => new ZeptoReciepient()
                    {
                        Email_address = new ZeptoEmailAddress()
                        {
                            Address = x.Email,
                            Name = x.Name
                        }
                    }).ToList() : null,
                    Cc = Cc is not null && Cc.Count > 0 ? Cc.Select(x => new ZeptoReciepient()
                    {
                        Email_address = new ZeptoEmailAddress()
                        {
                            Address = x.Email,
                            Name = x.Name
                        }
                    }).ToList() : null
                });

                sendResult = response.Status;
            }
            else
            {
                var emailSendResult = await mailJetService.SendMail(registeredMontyUser.SenderEmail,registeredMontyUser.SenderName, recipientAddress, message, subject, token, isHtml, Bcc, Cc);

                sendResult = emailSendResult;
            }
            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipientAddress,
                Subject = subject,
                Message = message,
                SentAt = DateTime.UtcNow
            }, token);

            return sendResult;
        }

        public async Task<bool> SendTransactionViaMailJet(string recipientAddress, string subject, int? templateId,
            BaseMailjetVariable variable, CancellationToken token, int numberOfDigits, string senderName = null, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), token);

            if (registeredMontyUser == null)
                return false;

            var templatedEmailSendResult = await mailJetService.SendMailWithTemplate(registeredMontyUser.SenderEmail,
                senderName ?? registeredMontyUser.SenderName, recipientAddress, subject, templateId.Value, variable, Bcc: Bcc, Cc: Cc);

            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipientAddress,
                TemplateId = templateId.ToString(),
                Subject = subject,
                Message = System.Text.Json.JsonSerializer.Serialize(variable),
                SentAt = DateTime.UtcNow
            }, token);

            return templatedEmailSendResult;
        }

        public async Task<bool> SendDynamicEmailViaMailjet(string recipientAddress, string subject, int templateId,
            object variable, CancellationToken token, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), token);

            if (registeredMontyUser == null)
                return false;

            var templatedEmailSendResult = await mailJetService.SendDynamicVariableMailWithTemplate(registeredMontyUser.SenderEmail, 
                registeredMontyUser.SenderName, recipientAddress, subject, templateId, variable, Bcc: Bcc, Cc: Cc);

            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipientAddress,
                TemplateId = templateId.ToString(),
                Subject = subject,
                Message = System.Text.Json.JsonSerializer.Serialize(variable),
                SentAt = DateTime.UtcNow
            }, token);

            return templatedEmailSendResult;
        }

        public async Task<bool> SendWithAttachmentsViaMailJet(string recipientAddress, string message, string subject, List<CreateMailJetAttachmentDto> attachments, CancellationToken token, string cc, bool isHtml = false)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), token);

            if (registeredMontyUser == null)
                return false;

            var emailSendResult = await mailJetService.SendMailWithAttachment(registeredMontyUser.SenderEmail, registeredMontyUser.SenderName, recipientAddress, message, subject, attachments, token, cc, isHtml);

            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipientAddress,
                Subject = subject,
                Message = message,
                SentAt = DateTime.UtcNow
            }, token);

            return emailSendResult;
}
        public async Task<bool> SendOTP(string recipient, CancellationToken cancellation, int numberOfDigits)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), cancellation);

            if (registeredMontyUser == null)
                return false;

            var generatedCode = await oTPService.GenerateOTP("Email", recipient, registeredMontyUser.UserId, numberOfDigits);

            if (registeredMontyUser.MailJetOtpId == null)
            {
                var emailSendResult = await SendViaMailJet(recipient, "Dear user, \n \n " +
                    $"kindly use the code {generatedCode} as your One-Time-Password. \n \n" +
                    $"Expires in 10 minutes.", "OTP Verification", cancellation);
                //return await SendOTP(recipient, cancellation);

                await SaveSentEmail(new SentEmail()
                {
                    RecipientAddress = recipient,
                    Subject = "OTP Verification",
                    Message = $"Dear user, kindly use the code {generatedCode} as your One-Time-Password. Expires in 10 minutes.",
                    SentAt = DateTime.UtcNow
                }, cancellation);

                return emailSendResult;
            }

            var templatedEmailSendResult = await mailJetService.SendMailWithOtpTemplate(registeredMontyUser.SenderEmail, registeredMontyUser.SenderName,
                recipient, "OTP Verification", Convert.ToInt32(registeredMontyUser.MailJetOtpId), new MailjetOtpTemplate() { Code = generatedCode, UserName = "" });

            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipient,
                Subject = "OTP Verification",
                TemplateId = registeredMontyUser.MailJetOtpId,
                Message = System.Text.Json.JsonSerializer.Serialize(new MailjetOtpTemplate() { Code = generatedCode, UserName = "" }),
                SentAt = DateTime.UtcNow
            }, cancellation);

            return templatedEmailSendResult;
            //return await mailJetService.SendOTP(recipient, registeredMontyUser.UserId, registeredMontyUser.SenderEmail, registeredMontyUser.SenderName);
        }

        public async Task<bool> SendOTPWithGeneratedCode(string recipient, string code, CancellationToken cancellation, int numberOfDigits)
        {
            var registeredMontyUser = await sesService.GetSesAccount(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), cancellation);

            if (registeredMontyUser == null)
                return false;

            if (string.IsNullOrWhiteSpace(registeredMontyUser.MailJetOtpId))
            {
                return await SendOTP(recipient, cancellation, numberOfDigits);
            }

            var templatedEmailSendResult = await mailJetService.SendMailWithOtpTemplate(registeredMontyUser.SenderEmail, registeredMontyUser.SenderName, recipient,
                "OTP Verification", Convert.ToInt32(registeredMontyUser.MailJetOtpId), new MailjetOtpTemplate() { Code = code, UserName = recipient });

            await SaveSentEmail(new SentEmail()
            {
                RecipientAddress = recipient,
                Subject = "OTP Verification",
                TemplateId = registeredMontyUser.MailJetOtpId,
                Message = System.Text.Json.JsonSerializer.Serialize(new MailjetOtpTemplate() { Code = code, UserName = recipient }),
                SentAt = DateTime.UtcNow
            }, cancellation);

            return templatedEmailSendResult;

            //var message = $"Kindly use code {code} for your One-Time-Password. Expires in 10 minutes.";
            //var subject = "Verification OTP";

            //return await mailJetService.SendMail(registeredMontyUser.SenderEmail, registeredMontyUser.SenderName, recipient, message, subject, cancellation);
            //return await mailJetService.SendOTPWithGeneratedCode(recipient, registeredMontyUser.UserId, registeredMontyUser.SenderEmail, registeredMontyUser.SenderName, code);
        }

        public async Task<(bool status, string message)> VerifyOTP(string recipient, string otp)
        {
            return await mailJetService.VerifyOTP(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), recipient, otp);
        }

        private async Task<bool> SaveEmailNotification(EmailNotification notification, CancellationToken token)
        {
            return await repository.AddAsync(notification, token);
        }

        private async Task<bool> SaveSentEmail(SentEmail email, CancellationToken token)
        {
            return await repository.AddAsync(email, token);
        }
    }
}
