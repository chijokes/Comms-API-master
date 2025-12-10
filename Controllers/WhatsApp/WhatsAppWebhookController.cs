using FusionComms.DTOs.WhatsApp;
using FusionComms.Services.WhatsApp;
using FusionComms.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;


namespace FusionComms.Controllers.WhatsApp
{
    [ApiController]
    [Route("api/v1/[controller]/{appId}")]
    public class WebhooksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly WebhookProcessorFactory _webhookProcessorFactory;
        
        public WebhooksController(AppDbContext dbContext, WebhookProcessorFactory webhookProcessorFactory)
        {
            _db = dbContext;
            _webhookProcessorFactory = webhookProcessorFactory;
        }

        [HttpGet]
        public async Task<IActionResult> VerifyWebhook(
            [FromRoute] string appId,
            [FromQuery] WhatsAppWebhookVerificationRequest request)
        {
            var appConfig = await _db.WhatsAppAppConfigs
                .FirstOrDefaultAsync(ac => ac.AppId == appId);

            if (appConfig == null)
                return BadRequest("Invalid appId");

            return request.HubMode == "subscribe" &&
                request.HubVerifyToken == appConfig.VerifyToken
                ? Ok(request.HubChallenge)
                : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook([FromRoute] string appId)
        {
            var payload = await ReadRequestBody();
            
            var parsedPayload = JObject.Parse(payload);
            var phoneNumberId = parsedPayload["entry"]?[0]?["changes"]?[0]?["value"]?["metadata"]?["phone_number_id"]?.ToString();
            
            if (string.IsNullOrEmpty(phoneNumberId))
            {
                return BadRequest("Phone number ID not found in webhook payload");
            }

            var appConfig = await _db.WhatsAppAppConfigs
                .FirstOrDefaultAsync(ac => ac.AppId == appId);

            if (appConfig == null)
            {
                return BadRequest($"Invalid appId: {appId}");
            }

            var business = await _db.WhatsAppBusinesses
                .FirstOrDefaultAsync(b => b.PhoneNumberId == phoneNumberId);

            if (business == null)
            {
                return BadRequest($"No business found for phoneNumberId: {phoneNumberId}");
            }

            if (!ValidateSignature(payload, appConfig.AppSecret)) 
            {
                return Unauthorized();
            }
            
            var processor = _webhookProcessorFactory.GetProcessor(business.BusinessType);
            return await processor.ProcessWebhook(business, payload);
        }

        private async Task<string> ReadRequestBody()
        {
            Request.EnableBuffering();
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            Request.Body.Position = 0;
            return body;
        }

        private bool ValidateSignature(string payload, string appSecret)
        {
            var signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
            if (string.IsNullOrEmpty(signatureHeader)) return false;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return $"sha256={BitConverter.ToString(hash).Replace("-", "").ToLower()}" == signatureHeader;
        }
    }
}