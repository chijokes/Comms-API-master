using FusionComms.DTOs;
using FusionComms.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface ISmsService
    {
        Task<bool> SendToSingle(SendSMSDto sms, CancellationToken cancellationToken = default, string senderId = null);

        Task<bool> SendToMultiple(SendSMSToMultipleDto model, CancellationToken token);
        Task<(bool Status, string Code)> SendOTP(string phoneNumber, CancellationToken cancellation, int numberOfDigits);

        Task<(bool status, string message)> VerifyOTP(string phoneNumber, string otp);

        Task<List<SMSBySenderResponseDto>> GetSMSBySender(DateTime dateFrom, DateTime dateTo, SmsBySenderRequestDto model, CancellationToken token);

        Task<(bool Status, string Message)> CallBackPush(string montyId, string phoneNumber, string notificationStatus, CancellationToken token);
    }


    public class SmsService : ISmsService
    {
        private readonly HttpContext HttpContext;
        private readonly IRepository Repository;
        private readonly IMontyService montyService;

        public SmsService(IRepository repository, IMontyService montyService, IHttpContextAccessor contextAccessor)
        {
            this.HttpContext = contextAccessor.HttpContext;
            Repository = repository;
            this.montyService = montyService;
        }

        public async Task<List<SMSBySenderResponseDto>> GetSMSBySender(DateTime dateFrom, DateTime dateTo, SmsBySenderRequestDto model, CancellationToken token)
        {
            return await Repository.ListAll<SMSNotification>()
                .Where(c => c.SenderAccountId == model.AccountId)
               .Where(c => c.CreatedAt.Date >= dateFrom && c.CreatedAt.Date <= dateTo)
               .Select(c => new SMSBySenderResponseDto { Text = c.Text, To = c.Receiver, SenderName = c.SenderName, SenderAccountId = c.SenderAccountId, CreatedAt = c.CreatedAt })
               .OrderByDescending(c => c.CreatedAt)
               .ToListAsync(token);
        }

        public async Task<(bool Status, string Code)> SendOTP(string phoneNumber, CancellationToken cancellation, int numberOfDigits)
        {
            return await montyService.SendOTP(phoneNumber, HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), numberOfDigits);
        }

        public async Task<bool> SendToMultiple(SendSMSToMultipleDto model, CancellationToken token)
        {
            var registeredMontyUser = await montyService.GetMontyUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (registeredMontyUser == null)
                return false;

            var result = await montyService.SendSMSToMultiple(new SMSToMultipleViaMontyDto()
            {
                Destination = model.Receiver,
                MontyAPIId = registeredMontyUser.MontyAPIId,
                MontyAuthCode = registeredMontyUser.MontyAuthCode,
                MontyPassword = registeredMontyUser.MontyPassword,
                MontyUserName = registeredMontyUser.MontyUserName,
                Source = registeredMontyUser.User.UserName,
                Text = model.Message,
                ViaResellerPlatform = registeredMontyUser.User.IsAResller
            });

            var notificationToAdd = new List<SMSNotification>();
            foreach(var receiver in model.Receiver)
            {
                notificationToAdd.Add(new SMSNotification()
                {
                    CreatedAt = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    SenderName = registeredMontyUser.User.UserName,
                    Text = model.Message,
                    Receiver = receiver,
                    SenderAccountId = registeredMontyUser.UserId,
                    IsActive = true,
                    IsSentSuccessfully = result.Status
                });
            }

            await SaveNotifications(notificationToAdd, token);

            return result.Status;
        }

        public async Task<bool> SendToSingle(SendSMSDto sms, CancellationToken cancellationToken = default, string senderId = null)
        {
            var registeredMontyUser = senderId is null ? await montyService.GetMontyUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)) :
                 await montyService.GetMontyUser(senderId);

            if (registeredMontyUser == null)
                return false;

            var result = await montyService.SendSMS(new SMSViaMontyDto()
            {
                Destination = sms.Receiver,
                MontyAPIId = registeredMontyUser.MontyAPIId,
                MontyAuthCode = registeredMontyUser.MontyAuthCode,
                MontyPassword = registeredMontyUser.MontyPassword,
                MontyUserName = registeredMontyUser.MontyUserName,
                Source = registeredMontyUser.User.UserName,
                Text = sms.Message,
                ViaResellerPlatform = registeredMontyUser.User.IsAResller
            });


            await SaveNotification(new SMSNotification()
            {
                CreatedAt = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString(),
                SenderName = registeredMontyUser.User.UserName,
                Text = sms.Message,
                Receiver = sms.Receiver,
                SenderAccountId = registeredMontyUser.UserId,
                IsActive = true,
                IsSentSuccessfully = result.Status,
                IsDelivered = false,
                MontyNotificationId = result.Id
            }, cancellationToken);

            return result.Status;
        }

        public async Task<(bool status, string message)> VerifyOTP(string phoneNumber, string otp)
        {
            return await montyService.VerifyOTP(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), phoneNumber, otp);
        }

        private async Task<bool> SaveNotification(SMSNotification notification, CancellationToken token)
        {
            return await Repository.AddAsync(notification, token);
        }

        private async Task<bool> SaveNotifications(List<SMSNotification> notifications, CancellationToken token)
        {
            return await Repository.AddRangeAsync(notifications, token);
        }

        public async Task<(bool Status, string Message)> CallBackPush(string montyId, string phoneNumber, string notificationStatus, CancellationToken token)
        {
            var notification = await Repository.ListAll<SMSNotification>()
                .Where(c => c.MontyNotificationId == montyId && c.Receiver == phoneNumber)
                .FirstOrDefaultAsync(token);

            if(notification == null)
            {
                return (false, "Sms not found in our system");
            }

            var registeredMontyUser = await montyService.GetMontyUser(notification.SenderAccountId);

            if(notification is null)
            {
                return (false, "SMS Notification not found");
            }
            else
            {
                if(notificationStatus.Equals("success", StringComparison.OrdinalIgnoreCase))
                {
                    notification.IsDelivered = true;
                    if(await Repository.ModifyAsync(notification, token))
                    {
                        return (true, "success");
                    }
                    else
                    {
                        return (false, "failed");
                    }
                }
                else
                {
                    var sendResult = await SendToSingle(new SendSMSDto()
                    {
                        Message = notification.Text,
                        Receiver = phoneNumber
                    }, token, registeredMontyUser.UserId);

                    if (sendResult)
                    {
                        return (true, "success");
                    }
                    else
                    {
                        return (false, "failed to resend sms");
                    }
                }
            }
        }
    }
}
