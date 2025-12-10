using FusionComms.DTOs;
using FusionComms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IMontyService
    {
        Task<(bool Status, string Id)> SendSMS(SMSViaMontyDto data, CancellationToken cancellationToken = default);
        Task<(bool Status, List<string> Ids)> SendSMSToMultiple(SMSToMultipleViaMontyDto data, CancellationToken cancellationToken = default);

        Task<RegisteredMontyUser> GetMontyUser(string userId);

        Task<List<RegisteredMontyUser>> GetRegisteredMontyUsers();

        Task<bool> Add(RegisteredMontyUser montyUser);

        Task<(bool Status, string Code)> SendOTP(string phoneNumber, string senderID, int numberOfDigits);

        Task<(bool, string)> VerifyOTP(string senderId, string phoneNumber, string otp);
    }
    public class MontyService : IMontyService
    {

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly IUserService userService;
        private readonly IOTPService otpService;
        private readonly IRepository repository;
        private readonly string montyBaseUrl;

        public MontyService(IConfiguration configuration, IRepository repository, IOTPService otpService, IUserService userService)
        {
            montyBaseUrl = configuration.GetSection("SMS:Providers:Monty:BaseUrl").Get<string>();
            this.repository = repository;
            this.otpService = otpService;
            this.userService = userService;
        }

        public async Task<RegisteredMontyUser> GetMontyUser(string userId)
        {
            return await repository.ListAll<RegisteredMontyUser>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<bool> Add(RegisteredMontyUser montyUser)
        {
            if (montyUser is null)
                return false;

            return await repository.AddAsync(montyUser);
        }

        public async Task<(bool Status, string Id)> SendSMS(SMSViaMontyDto model, CancellationToken cancellationToken = default)
        {
            using (var _httpClient = new HttpClient())
            {
                if (model.ViaResellerPlatform == false)
                {
                    //Uri uriStep = new Uri("https://httpsmsc05.montymobile.com/HTTP/api/Client/SendSMS");
                    Uri uriStep = new Uri("https://bulksms.fusionintel.io/API/SendBulkSMS");

                    _httpClient.DefaultRequestHeaders.Add("Username", model.MontyUserName);
                    _httpClient.DefaultRequestHeaders.Add("Password", model.MontyPassword);
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {model.MontyAPIId}");
                    _httpClient.DefaultRequestHeaders.Add("X-Access-Token", $"{model.MontyAuthCode}");


                    HttpResponseMessage res = await _httpClient.PostAsJsonAsync(uriStep, model, cancellationToken);
                    var response = await res.Content.ReadAsStringAsync();

                    var deserializedResponse = JsonSerializer.Deserialize<List<MontySmsResponse>>(response, options);

                    if (deserializedResponse[0].ErrorCode == 0)
                    {
                        return (true, deserializedResponse[0].Id);
                    }

                    return (false, null);
                }

                Uri uri = new Uri($"{montyBaseUrl}?username={model.MontyUserName}&apiId={model.MontyAPIId}&json=True&destination={model.Destination}&source={model.Source}&text={model.Text}");

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.MontyAuthCode}");


                HttpResponseMessage getResponse = await _httpClient.GetAsync(uri, cancellationToken);
                var responseResult = await getResponse.Content.ReadAsStringAsync();

                var deserializedGetResponse = JsonSerializer.Deserialize<MontySmsResponse>(responseResult);

                if (deserializedGetResponse.ErrorCode == 0)
                {
                    return (true, deserializedGetResponse.Id);
                }

                return (false, null);
            }
        }

        public async Task<List<RegisteredMontyUser>> GetRegisteredMontyUsers()
        {
            return await repository.ListAll<RegisteredMontyUser>().ToListAsync();
        }

        public async Task<(bool Status, string Code)> SendOTP(string phoneNumber, string senderID, int numberOfDigits)
        {
            var generatedOTP = await otpService.GenerateOTP(OTPChannels.MontySms, phoneNumber, senderID, numberOfDigits);

            if(generatedOTP == null)
            {
                return (false, string.Empty);
            }

            var user = await repository.ListAll<RegisteredMontyUser>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == senderID);

            if(user is null)
            {
                return (false, null);
            }

           var result = await SendSMS(new SMSViaMontyDto()
            {
                Destination = phoneNumber,
                MontyAPIId = user.MontyAPIId,
                MontyAuthCode = user.MontyAuthCode,
                MontyPassword = user.MontyPassword,
                MontyUserName = user.MontyUserName,
                Source = user.Source,
                Text = $"Kindly use code {generatedOTP} for your One-Time-Password. Expires in 10 minutes.",
                ViaResellerPlatform = user.User.IsAResller
            });



            return (result.Status, generatedOTP);
        }

        public async Task<(bool, string)> VerifyOTP(string senderId, string phoneNumber, string otp)
        {
            return await otpService.VerifyOTP(senderId, phoneNumber, otp);
        }

        public async Task<(bool Status, List<string> Ids)> SendSMSToMultiple(SMSToMultipleViaMontyDto model, CancellationToken cancellationToken = default)
        {
            using (var _httpClient = new HttpClient())
            {
                if (model.ViaResellerPlatform == false)
                {
                    //Uri uriStep = new Uri("https://httpsmsc05.montymobile.com/HTTP/api/Client/SendSMS");
                    Uri uriStep = new Uri("https://bulksms.fusionintel.io/API/SendBulkSMS");

                    _httpClient.DefaultRequestHeaders.Add("Username", model.MontyUserName);
                    _httpClient.DefaultRequestHeaders.Add("Password", model.MontyPassword);
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {model.MontyAuthCode}");


                    HttpResponseMessage res = await _httpClient.PostAsJsonAsync(uriStep, model, cancellationToken);
                    var response = await res.Content.ReadAsStringAsync();

                    var deserializedResponse = JsonSerializer.Deserialize<List<MontySmsResponse>>(response, options);

                    if (deserializedResponse[0].ErrorCode == 0)
                    {
                        return (true, deserializedResponse.Select(c => c.Id).ToList());
                    }

                    return (false, null);
                }

                Uri uri = new Uri($"{montyBaseUrl}?username={model.MontyUserName}&apiId={model.MontyAPIId}&json=True&destination={model.Destination}&source={model.Source}&text={model.Text}");

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.MontyAuthCode}");


                HttpResponseMessage getResponse = await _httpClient.GetAsync(uri, cancellationToken);
                var responseResult = await getResponse.Content.ReadAsStringAsync();

                var deserializedGetResponse = JsonSerializer.Deserialize<List<MontySmsResponse>>(responseResult);

                if (deserializedGetResponse.Count > 0)
                {
                    return (true, deserializedGetResponse.Select(c => c.Id).ToList());
                }

                return (false, null);
            }
        }
    }
}
