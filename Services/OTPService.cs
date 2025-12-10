using FusionComms.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IOTPService
    {
        Task<string> GenerateOTP(string channel, string phoneNumber, string senderId, int numberOfDigits);

        Task<string> CheckExistingOTP(string phoneNumber, string senderId);

        Task<(bool, string)> VerifyOTP(string senderId, string phoneNumber, string otp);
    }


    public class OTPService : IOTPService
    {
        private IRepository repository;

        public OTPService(IRepository repository)
        {
            this.repository = repository;
        }

        public async Task<string> CheckExistingOTP(string phoneNumber, string senderId)
        {
            var existingOTP= await repository.ListAll<OTP>()
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(c => c.ExpiryDate > DateTime.UtcNow && c.PhoneNumber == phoneNumber && c.SenderAccountId == senderId && c.IsConfirmed == false);

            if (existingOTP != null)
                return existingOTP.Code;
            return null;
        }

        public async Task<string> GenerateOTP(string channel, string phoneNumber, string senderId, int numberOfDigits)
        {
            //This is to ensure that we don't generate new otps and populate the OTP tables with unsed OTPs
            string generatedCode;

            generatedCode = await CheckExistingOTP(phoneNumber, senderId);

            if(generatedCode is null || generatedCode.Length != numberOfDigits)
            {
                generatedCode = GenerateOTPCode(numberOfDigits);

                 await repository.AddAsync<OTP>(new OTP()
                {
                    Channel = channel,
                    Code = generatedCode,
                    PhoneNumber = phoneNumber,
                    SenderAccountId = senderId,
                });
            }

            return generatedCode;
        }

        public async Task<(bool, string)> VerifyOTP(string senderId, string phoneNumber, string otp)
        {
            var existingOTP = await repository.ListAll<OTP>()
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(c => c.Code == otp && c.SenderAccountId == senderId && Regex.IsMatch(c.PhoneNumber, phoneNumber, RegexOptions.IgnoreCase));

            if(existingOTP is null)
            {
                return (false, "Invalid OTP");
            }

            var checkUniTime = existingOTP.ExpiryDate.Kind == DateTimeKind.Utc || existingOTP.ExpiryDate.Kind == DateTimeKind.Unspecified 
                ? existingOTP.ExpiryDate : existingOTP.ExpiryDate.ToUniversalTime();

            if (existingOTP is null)    
            {
                return (false, "Invalid OTP");
            }
            else if (DateTime.UtcNow > checkUniTime)
            {
                return (false, "Otp expired");
            }
            else if (existingOTP.IsConfirmed)
            {
                return (false, "OTP has already been confirmed");
            }
            else if(existingOTP.Code != otp)
            {
                return (false, "Invalid OTP");
            }
            else
            {
                //need to check this piece and redo it appropirately
                if(existingOTP.Code == otp)
                {
                    existingOTP.IsConfirmed = true;
                    await repository.ModifyAsync(existingOTP);
                    return (true, "OTP confirmed");
                }

                return (false, "Error verifying otp");
            }
        }

        private string GenerateOTPCode(int numberOfDigits = 6)
        {
            var random = Guid.NewGuid().ToString();
            return string.Join("", new Regex("[0-9]").Matches(random)).Substring(0, numberOfDigits);
        }
    }
}
