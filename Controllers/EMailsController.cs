using FusionComms.DTOs;
using FusionComms.Services;
using FusionComms.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EMailsController : ControllerBase
    {
        private readonly IEmailService emailService;
        private readonly IMailJetService mailJetService;
        private readonly IZeptoMailService _zeptoMailService;
        private readonly IUserService userService;

        public EMailsController(IUserService userService, IEmailService emailService, IMailJetService mailJetService, IZeptoMailService zeptoMailService)
        {
            this.userService = userService;
            this.emailService = emailService;
            this.mailJetService = mailJetService;
            _zeptoMailService = zeptoMailService;
        }
        
        [HttpPost("SendToSingle")]
        [CustomAuthorizer("Send Single Email", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendToSingle(SendEmailDto smsModel, CancellationToken token)
        {
            if (ModelState.IsValid)
            {
                var result = await emailService.SendToSingle(smsModel, token);

                if (result == true)
                {
                    return Ok(Util.BuildResponse(200, "Ok", null, "Email Sent Successfully"));
                }

                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
        }


        [HttpPost("send-via-mailjet")]
        [CustomAuthorizer("Send Single Email Via MailJet", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendViaMailJet(SendEmailViaMailJetDto model, CancellationToken token)
        {
            return Ok(await emailService.SendViaMailJet(model.ReciepeintAddress, model.Message, model.Subject, token, model.IsHtml, model.Bcc, model.Cc));
        }


        [HttpPost("send-transcational-email-via-mailjet")]
        [CustomAuthorizer("Send Single Email Via MailJet", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendMaiViaTempllate(int templateId, SendTemplateEmail model, CancellationToken token, int numberOfDigits = 6)
        {
            var result = await emailService.SendTransactionViaMailJet(model.ReciepeintAddress, model.Subject, templateId, new BaseMailjetVariable()
            {
                UserName = model.UserName,
                CustomMessage = model.Message
            }, token, numberOfDigits, model.SenderName, Bcc: model.Bcc, Cc: model.Cc);

            if (result)
            {
                return Ok();
            }
            else
            {
                return UnprocessableEntity();
            }
        }

        [HttpPost("send-dynamic-email-via-mailjet")]
        [CustomAuthorizer("Send Single Email Via MailJet", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendDynamicMaiViaTempllate(int templateId, DynamicMailTemplate model, CancellationToken token)
        {
            var result = await emailService.SendDynamicEmailViaMailjet(model.ReciepeintAddress, model.Subject, templateId,
                variable: model.Variable,
            token, Bcc: model.Bcc, Cc: model.Cc);

            if (result)
            {
                return Ok();
            }
            else
            {
                return UnprocessableEntity();
            }
        }


        [HttpPost("send-custom-email-via-mailjet")]
        [CustomAuthorizer("Send Single Email Via MailJet", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendMaiViaTempllateV2(int templateId, SendWithCustomMessage model, CancellationToken token, int numberOfDigits = 6)
        {
            var result = await emailService.SendTransactionViaMailJet(model.ReciepeintAddress, model.Subject, templateId, new BaseMailjetVariable()
            {
                CustomMessage = model.Message
            }, token, numberOfDigits, Bcc: model.Bcc, Cc: model.Cc);

            if (result)
            {
                return Ok();
            }
            else
            {
                return UnprocessableEntity();
            }
        }

        [HttpGet("send-otp")]
        [CustomAuthorizer("Send OTP", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendOTP(string recipient, CancellationToken token, int numberOfDigts = 6)
        {

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return BadRequest("Invalid recipient address");
            }
            
            var user = await userService.CheckUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (user is null)
            {
                return BadRequest("Sender Account not found");
            }

            var result = await emailService.SendOTP(recipient, token, numberOfDigts);

            if (result == true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, "OTP Sent Successfully"));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", null, "Error sending OTP"));
        }

        [HttpGet("verify-otp")]
        [CustomAuthorizer("Verify OTP", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> VerifyOTP(string recipient, string otp, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                return BadRequest("Invalid recipient address");
            }


            var user = await userService.CheckUser(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (user is null)
            {
                return BadRequest("Sender Account not found");
            }

            var result = await emailService.VerifyOTP(recipient, otp);

            if (result.status == true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, result.message));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", null, result.message));
        }


        [HttpPost("send-with-attachments-via-mailjet")]
        [CustomAuthorizer("Send Single Email Via MailJet", AuthorizationCheckType.AuthorizeByUser)]
        public async Task<IActionResult> SendwithAttachmentsViaMailJet([FromForm]SendEmailViaMailJetWithAttachmentsDto model, CancellationToken token)
        {
            return Ok(await emailService.SendWithAttachmentsViaMailJet(model.ReciepeintAddress, model.Message, model.Subject, model.Files.Select(c => new CreateMailJetAttachmentDto()
            {
               FileName = c.FileName,
               FileToUpload = c
            }).ToList(), token, model.Cc, model.IsHtml));
        }
    }
}
