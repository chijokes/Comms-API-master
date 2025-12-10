using FusionComms.DTOs;
using FusionComms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IAmazonSESService
    {
        Task<bool> SendEMail(SendEMailViaSESDto model, CancellationToken token);

        Task<RegisteredSesUser> GetSesAccount(string userId, CancellationToken token);

        Task<List<RegisteredSesUser>> GetRegisteredSesUsers(CancellationToken token);

        Task<bool> Create(RegisteredSesUser model, CancellationToken token);

        //Task<bool> SendOTP(string emailAddress, string senderId, CancellationToken token);

        //Task<(bool, string)> VerifyOTP(string senderId, string emailAddress, string OTP);
    }


    public class AmazonSESService : IAmazonSESService
    {

        private readonly IRepository repository;
        private readonly string SMTPUserName;
        private readonly string SMTPPassword;
        private readonly string SMPTHost;

        public AmazonSESService(IRepository repository, IConfiguration configuration)
        {
            SMTPUserName = configuration.GetSection("Email:Providers:AmazonSESSMTP:SMTPUserName").Get<string>();
            SMTPPassword = configuration.GetSection("Email:Providers:AmazonSESSMTP:SMTPPassword").Get<string>();
            SMPTHost = configuration.GetSection("Email:Providers:AmazonSESSMTP:SMTPHost").Get<string>();
            this.repository = repository;
        }

        public async Task<bool> Create(RegisteredSesUser model, CancellationToken token)
        {
            if (model is null)
                return false;

            return await repository.AddAsync(model, token);
        }

        public async Task<List<RegisteredSesUser>> GetRegisteredSesUsers(CancellationToken token)
        {
            return await repository.ListAll<RegisteredSesUser>().ToListAsync(token);
        }

        public async Task<RegisteredSesUser> GetSesAccount(string userId, CancellationToken token)
        {
            return await repository.ListAll<RegisteredSesUser>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId, token);
        }

        //work on refactoring this method nitori olorun
        public async Task<bool> SendEMail(SendEMailViaSESDto model, CancellationToken token)
        {

            MailMessage mailMessage = new MailMessage();
            mailMessage.To.Add(new MailAddress(model.Recepient));
            mailMessage.Subject = model.Subject;
            mailMessage.Body = model.Message;
            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(model.SenderAddress, model.SenderName);

            using(var client = new SmtpClient(SMPTHost, 25))
            {
                //client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(SMTPUserName, SMTPPassword);

                client.EnableSsl = true;

                //implement try and catch block here.
                await client.SendMailAsync(mailMessage, token);
            }

            return true;
        }

    }
}
