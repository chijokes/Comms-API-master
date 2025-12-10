using System.Threading.Tasks;
using FusionComms.Entities.WhatsApp;
using Microsoft.AspNetCore.Mvc;

namespace FusionComms.Services.WhatsApp
{
    public interface IWhatsAppWebhookProcessor
    {
        Task<IActionResult> ProcessWebhook(WhatsAppBusiness business, string payload);
    }
}