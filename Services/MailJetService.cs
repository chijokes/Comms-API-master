using FusionComms.DTOs;
using FusionComms.Entities;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace FusionComms.Services
{
    public interface IMailJetService
    {
        Task<bool> SendMail(string senderAddress, string senderName, string reciepientAddress, string message, string subject, CancellationToken token, bool isHtml = false, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null);
        Task<bool> SendOTP(string recipient, string senderId, string senderAddress, string senderName, int numberOfDigits);
        Task<bool> SendOTPWithGeneratedCode(string reciepientAddress, string senderId, string senderAddress, string senderName, string generatedOTP);
        Task<(bool, string)> VerifyOTP(string senderAddress, string recipient, string otp);

        Task<bool> SendMailWithAttachment(string senderAddress, string senderName, string reciepientAddress, string message, string subject,
            List<CreateMailJetAttachmentDto> attachments, CancellationToken token, string cc, bool isHtml = false);

        Task<bool> SendMailWithTemplate<T>(string senderAddress, string senderName, string reciepientAddress, string subject,
            int templateId, T variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null) where T : BaseMailjetVariable;
        Task<bool> SendMailWithOtpTemplate<T>(string senderAddress, string senderName, string reciepientAddress, string subject,
            int templateId, T variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null) where T : MailjetOtpTemplate;

        Task<bool> SendDynamicVariableMailWithTemplate(string senderAddress, string senderName, string reciepientAddress, string subject,
                int templateId, object variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null);
    }


    public class MailJetService : IMailJetService
    {
        private readonly IRepository repository;
        private readonly IMailjetClient mailjetClient;
        private readonly IOTPService otpService;
        public MailJetService(IRepository repository, IMailjetClient mailjetClient, IOTPService otpService)
        {
            this.repository = repository;
            this.mailjetClient = mailjetClient;
            this.otpService = otpService; 
        }

        public async Task<bool> SendMail(string senderAddress, string senderName, string reciepientAddress, string message, string subject, CancellationToken token, bool isHtml = false, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null)
        {

            var mailjetSendClass = new List<MailJetCustomProp>();
            var reciepients = new List<MailjetUserDetails>();
            reciepients.Add(new MailjetUserDetails()
            {
                Email = reciepientAddress,
                Name = reciepientAddress
            });


            mailjetSendClass.Add(new MailJetCustomProp()
            {
                From = new MailjetUserDetails()
                {
                    Name = senderName,
                    Email = senderAddress
                },
                HTMLPart = isHtml ? message : null,
                Subject = subject,
                TextPart = message,
                To = reciepients,
                Bcc = Bcc,
                Cc = Cc
            });

            var result = JsonSerializer.Serialize<List<MailJetCustomProp>>(mailjetSendClass);

            var arr = JArray.Parse(result);

            MailjetRequest request = new MailjetRequest()
            {
                Resource = new ResourceInfo("send", null, ApiVersion.V3_1, ResourceType.Send)
            }.Property(Send.Messages, arr);

            Log.Information("sending request to mailjet with body: {@Model}", result);
            var response = await mailjetClient.PostAsync(request);

            Log.Information("mailjet response {@Response}", response);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendDynamicVariableMailWithTemplate(string senderAddress, string senderName, string reciepientAddress, string subject,
                int templateId, object variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null)
        {
            var mailjetSendClass = new List<MailJetDynamicWithTemplate>();
            var reciepients = new List<MailjetUserDetails>
            {
                new MailjetUserDetails()
                {
                    Email = reciepientAddress,
                    Name = reciepientAddress
                }
            };


            mailjetSendClass.Add(new MailJetDynamicWithTemplate()
            {
                From = new MailjetUserDetails()
                {
                    Name = senderName,
                    Email = senderAddress
                },
                Subject = subject,
                TemplateId = templateId,
                TemplateLanguage = true,
                Variables = variables,
                To = reciepients,
                Bcc = Bcc,
                Cc = Cc
            });


            var result = JsonSerializer.Serialize(mailjetSendClass);

            var arr = JArray.Parse(result);

            MailjetRequest request = new MailjetRequest()
            {
                //Resource = Send.Resource
                Resource = new ResourceInfo("send", null, ApiVersion.V3_1, ResourceType.Send)
            }.Property(Send.Messages, arr);

            Log.Information("sending request to mailjet with body: {@Model}", result);
            
            var response = await mailjetClient.PostAsync(request);

            Log.Information("mailjet response {@Response}", response);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendMailWithTemplate<T>(string senderAddress, string senderName, string reciepientAddress, string subject,
            int templateId, T variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null) where T : BaseMailjetVariable
        {
            var mailjetSendClass = new List<MailJetWithTemplate<BaseMailjetVariable>>();
            var reciepients = new List<MailjetUserDetails>
            {
                new MailjetUserDetails()
                {
                    Email = reciepientAddress,
                    Name = reciepientAddress
                }
            };


            mailjetSendClass.Add(new MailJetWithTemplate<BaseMailjetVariable>()
            {
                From = new MailjetUserDetails()
                {
                    Name = senderName,
                    Email = senderAddress
                },
                Subject = subject,
                TemplateId = templateId,
                TemplateLanguage = true,
                Variables = variables,
                To = reciepients,
                Bcc = Bcc,
                Cc = Cc
            });


            var result = JsonSerializer.Serialize<List<MailJetWithTemplate<BaseMailjetVariable>>>(mailjetSendClass);

            var arr = JArray.Parse(result);

            MailjetRequest request = new MailjetRequest()
            {
                //Resource = Send.Resource
                Resource = new ResourceInfo("send", null, ApiVersion.V3_1, ResourceType.Send)
            }.Property(Send.Messages, arr);

            Log.Information("sending request to mailjet with body: {@Model}", result);
            
            var response = await mailjetClient.PostAsync(request);
            Log.Information("mailjet response {@Response}", response);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendMailWithOtpTemplate<T>(string senderAddress, string senderName, string reciepientAddress, string subject,
            int templateId, T variables, List<MailjetUserDetails> Bcc = null, List<MailjetUserDetails> Cc = null) where T : MailjetOtpTemplate
        {
            var mailjetSendClass = new List<MailJetWithTemplate<MailjetOtpTemplate>>();
            var reciepients = new List<MailjetUserDetails>();
            reciepients.Add(new MailjetUserDetails()
            {
                Email = reciepientAddress,
                Name = reciepientAddress
            });


            mailjetSendClass.Add(new MailJetWithTemplate<MailjetOtpTemplate>()
            {
                From = new MailjetUserDetails()
                {
                    Name = senderName,
                    Email = senderAddress
                },
                //HTMLPart = "Hello",
                Subject = subject,
                TemplateId = templateId,
                TemplateLanguage = true,
                Variables = new MailjetOtpTemplate() { Code = variables.Code, UserName = "User" },
                To = reciepients
            });

            //var messageToSend = new MailJetSenderClass();
            //messageToSend.Messages = mailjetSendClass;


            var result = JsonSerializer.Serialize<List<MailJetWithTemplate<MailjetOtpTemplate>>>(mailjetSendClass);

            var arr = JArray.Parse(result);

            MailjetRequest request = new MailjetRequest()
            {
                //Resource = Send.Resource
                Resource = new ResourceInfo("send", null, ApiVersion.V3_1, ResourceType.Send)
            }.Property(Send.Messages, arr);
            Log.Information("sending request to mailjet with body: {@Model}", result);

            var response = await mailjetClient.PostAsync(request);
            Log.Information("mailjet response {@Response}", response);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendOTP(string reciepientAddress, string senderId, string senderAddress, string senderName, int numberOfDigits)
        {
            var generatedOTP = await otpService.GenerateOTP(OTPChannels.MontySms, reciepientAddress, senderId, numberOfDigits);

            if (generatedOTP == null)
            {
                return false;
            }

            var message = $"Kindly use code {generatedOTP} for your One-Time-Password. Expires in 10 minutes.";
            var subject = "Verification OTP";
            
            
            

            return await SendMail(senderAddress, senderName ,reciepientAddress, message, subject, default);
            
            //return await SendMail(new SendEmailViaMailJetDto()
            //{
            //    ReciepeintAddress = reciepientAddress,
            //    Subject = "Verification OTP",
            //    Message = $"Kindly use code {generatedOTP} for your One-Time-Password. Expires in 10 minutes.",
            //});

        }


        public async Task<bool> SendOTPWithGeneratedCode(string reciepientAddress, string senderId, string senderAddress, string senderName, string generatedOTP)
        {
            //var generatedOTP = await otpService.GenerateOTP(OTPChannels.MontySms, reciepientAddress, senderId);

            //if (generatedOTP == null)
            //{
            //    return false;
            //}

            var message = $"Kindly use code {generatedOTP} for your One-Time-Password. Expires in 10 minutes.";
            var subject = "Verification OTP";




            return await SendMail(senderAddress, senderName, reciepientAddress, message, subject, default);

            //return await SendMail(new SendEmailViaMailJetDto()
            //{
            //    RecipientAddress  = reciepientAddress,
            //    Subject = "Verification OTP",
            //    Message = $"Kindly use code {generatedOTP} for your One-Time-Password. Expires in 10 minutes.",
            //});

        }

        public async Task<(bool, string)> VerifyOTP(string senderAddress, string recipient, string otp)
        {
            return await otpService.VerifyOTP(senderAddress, recipient, otp);
            }

        public async Task<bool> SendMailWithAttachment(string senderAddress, string senderName, string reciepientAddress, string message, string subject,
            List<CreateMailJetAttachmentDto> attachments, CancellationToken token, string cc, bool isHtml = false)
        {

            var mailjetSendClass = new List<MailJetCustomProp>();
            var reciepients = new List<MailjetUserDetails>();
            reciepients.Add(new MailjetUserDetails()
            {
                Email = reciepientAddress,
                Name = reciepientAddress
            });

            // I just had to #tearsss
            List<MailjetUserDetails> mailjetUserDetails = new();

            if (!string.IsNullOrEmpty(cc))
            {
                var splitString = cc.Split(',');
                mailjetUserDetails.AddRange(splitString.Select(c => new MailjetUserDetails()
                {
                    Email = c.Trim(),
                    Name = c.Trim(),
                }));
            }

            var mailjetAttachments = new List<MailJetAttachment>();

            foreach (var attachment in attachments)
            {
                string baseContent = string.Empty;

                if (attachment.FileToUpload.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        attachment.FileToUpload.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        baseContent = Convert.ToBase64String(fileBytes);
                    }
                }

                mailjetAttachments.Add(new MailJetAttachment()
                {
                    Filename = attachment.FileName,
                    ContentType = "text/plain",
                    Base64Content = baseContent
                });
            }

            mailjetSendClass.Add(new MailJetCustomProp()
            {
                From = new MailjetUserDetails()
                {
                    Name = senderName,
                    Email = senderAddress
                },
                HTMLPart = isHtml ? message : null,
                Subject = subject,
                TextPart = message,
                To = reciepients,
                Attachments = mailjetAttachments,
                Cc = mailjetUserDetails
            });

            //var messageToSend = new MailJetSenderClass();
            //messageToSend.Messages = mailjetSendClass;


            var result = JsonSerializer.Serialize<List<MailJetCustomProp>>(mailjetSendClass);

            var arr = JArray.Parse(result);

            MailjetRequest request = new MailjetRequest()
            {
                //Resource = Send.Resource
                Resource = new ResourceInfo("send", null, ApiVersion.V3_1, ResourceType.Send)
            }.Property(Send.Messages, arr);
            Log.Information("sending request to mailjet with body: {@Model}", result);

            var response = await mailjetClient.PostAsync(request);
            Log.Information("mailjet response {@Response}", response);

            return response.IsSuccessStatusCode;
        }
    }
}
