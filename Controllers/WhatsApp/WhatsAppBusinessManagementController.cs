using FusionComms.Data;
using FusionComms.Services.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Controllers.WhatsApp
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppBusinessManagementController : ControllerBase
    {
        private readonly IWhatsAppTemplateService _templateService;
        private AppDbContext _db;

        public WhatsAppBusinessManagementController(IWhatsAppTemplateService templateService, AppDbContext db)
        {
            _templateService = templateService;
            _db = db;
        }

        [HttpPost("CreateTemplate")]
        [Authorize]
        public async Task<IActionResult> CreateTemplate( WhatsappTemplateConstants function, string waBaId, CancellationToken token)
        {
            var businessToken = await _db.WhatsAppBusinesses.Where(c => c.AccountId == waBaId).Select(c=>c.BusinessToken).FirstOrDefaultAsync(token);

            var result = await _templateService.CreateTemplate(function,waBaId,businessToken);
            if (result.Success) { return Ok(result); }
            return BadRequest(result);
        }

        [HttpPost("CreateTemplateForAll")]
        [Authorize]
        public async Task<IActionResult> CreateTemplatesForAll([FromQuery] WhatsappTemplateConstants function)
        {
            var result = await _templateService.CreateTemplatesForAll(function);
            return Ok(result);
        }

        [HttpGet("ListAllBusinesses")]
        [Authorize]
        public async Task<IActionResult> ListAllBusinesses(CancellationToken token)
        {
            var businesses = await _db.WhatsAppBusinesses.Select(c=> new {c.BusinessName,c.BusinessId,c.EmbedlyAccountId,c.AccountId}).ToListAsync(token);
            return Ok(businesses);
        }

        

        [HttpPost("CreateEmbedlyId")]
        [Authorize]
        public async Task<IActionResult> AddEmbedlyId(string businessId, string EmbedlyId, CancellationToken token)
        {
            var business = await _db.WhatsAppBusinesses.Where(c => c.BusinessId == businessId).FirstOrDefaultAsync(token);
            if (business == null) { return NotFound(); }

            business.EmbedlyAccountId = EmbedlyId;
            try
            {
                await _db.SaveChangesAsync(token);
                return Ok(business);
            }catch(Exception e)
            {
                return StatusCode(500);
            }
        }
    }
}
