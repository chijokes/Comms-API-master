using System;
using FusionComms.Entities.WhatsApp;
using FusionComms.Services.WhatsApp.Cinemas;
using FusionComms.Services.WhatsApp.Restaurants;
using Microsoft.Extensions.DependencyInjection;

namespace FusionComms.Services.WhatsApp
{
    public class WebhookProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WebhookProcessorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWhatsAppWebhookProcessor GetProcessor(BusinessType businessType)
        {
            return businessType switch
            {
                BusinessType.Restaurant => _serviceProvider.GetRequiredService<RestaurantWebhookProcessor>(),
                BusinessType.Cinema => _serviceProvider.GetRequiredService<CinemaWebhookProcessor>(),
                _ => throw new NotSupportedException($"Business type '{businessType}' is not supported.")
            };
        }
    }
}
