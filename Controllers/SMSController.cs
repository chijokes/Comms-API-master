using FusionComms.DTOs;
using FusionComms.Services;
using FusionComms.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SMSController : ControllerBase
    {
        private readonly ISmsService smsService;
        private readonly IEmailService emailService;
        private readonly IUserService userService;

        public SMSController(ISmsService smsService, IUserService userService, IEmailService emailService)
        {
            this.smsService = smsService;
            this.userService = userService;
            this.emailService = emailService;
        }


        [HttpPost("SendToSingle")]
        [CustomAuthorizer("Send Single SMS", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendToSingle(SendSMSDto smsModel)
        {
            if (ModelState.IsValid)
            {
                var result = await smsService.SendToSingle(smsModel);

                if (result == true)
                {
                    return Ok(Util.BuildResponse(200, "Ok", null, "Sms Sent Successfully"));
                }

                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
        }


        [HttpPost("SendToMultiple")]
        [CustomAuthorizer("Send Single SMS", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendToMultiple(SendSMSToMultipleDto smsModel, CancellationToken token)
        {
            if (ModelState.IsValid)
            {
                var result = await smsService.SendToMultiple(smsModel, token);

                if (result == true)
                {
                    return Ok(Util.BuildResponse(200, "Ok", null, "Sms Sent Successfully"));
                }

                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
        }



        [HttpGet("send-otp")]
        [CustomAuthorizer("Send OTP", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendOTP(string phoneNumber, CancellationToken token, int numberOfDigits = 6, string email = null, bool sendToEmail = false)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest("Invalid Phone Number");
            }


            var user = await userService.CheckUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (user is null)
            {
                return BadRequest("Sender Account not found");
            }

            var result = await smsService.SendOTP(phoneNumber, token, numberOfDigits);

            if (result.Status == true)
            {
                if (sendToEmail)
                {
                    if(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier).ToLower().Contains("bloomers") is false)
                    {
                        await emailService.SendOTPWithGeneratedCode(email, result.Code, token, numberOfDigits);
                    }
                }
                return Ok(Util.BuildResponse(200, "Ok", null, "OTP Sent Successfully"));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", null, "Error sending OTP"));
        }



        [HttpGet("verify-otp")]
        [CustomAuthorizer("Verify OTP", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> VerifyOTP(string phoneNumber, string otp, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest("Invalid Phone Number");
            }


            var user = await userService.CheckUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (user is null)
            {
                return BadRequest("Sender Account not found");
            }

            var result = await smsService.VerifyOTP(phoneNumber, otp);

            if(result.status == true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, result.message));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", null, result.message));

        }



        [HttpGet("GetSMSBySender")]
        [CustomAuthorizer("List SMS By Sender", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> GetSMSBySender([FromQuery]SmsBySenderRequestDto request, CancellationToken token)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Parameters");
            }

            var senderAccountId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(senderAccountId is null)
            {
                return BadRequest("Invalid credentials");
            }

            DateTime? datefrom = String.IsNullOrWhiteSpace(request.DateFrom) ? null : DateTime.Parse(request.DateFrom);
            DateTime? dateto = String.IsNullOrWhiteSpace(request.DateTo) ? null : DateTime.Parse(request.DateTo);
            request.AccountId = senderAccountId;

            if(datefrom == null || dateto == null)
            {
                datefrom = DateTime.UtcNow.AddDays(-30);
                dateto = DateTime.UtcNow;
            }


            //this entire logic needs reworking
            //i need to allow datetime parse without casting it
            //also need to change the accepting date format at the top to DateTime, not String
            var smsResult = await smsService.GetSMSBySender((DateTime)datefrom, (DateTime)dateto, request, token);


            //also, this section needs rewriting
            return Ok(smsResult);
        }



        [HttpGet("Monty/Callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PushBackUrl([FromQuery]MontyCallBack model, CancellationToken token)
        {
            var pushBackResult = await smsService.CallBackPush(model.MessageId, model.MobileNo, model.Status, token);

            if(pushBackResult.Status == false)
            {
                return UnprocessableEntity(pushBackResult.Message);
            }
            else
            {
                return Ok(pushBackResult.Message);
            }
        }
    }
}
