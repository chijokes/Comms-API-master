using System.Threading.Tasks;
using FusionComms.Data;
using FusionComms.DTOs.WhatsApp;
using FusionComms.Services.WhatsApp;
using FusionComms.Services.WhatsApp.Restaurants;


namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class RestaurantWebhookProcessor : WhatsAppWebhookProcessor
    {
        private readonly OrderFlowEngine _orderFlowEngine;
        private readonly OrderSessionManager _sessionManager;
        private readonly WhatsAppMessagingService _whatsAppMessaging;

        public RestaurantWebhookProcessor(
            AppDbContext dbContext,
            WhatsAppMessagingService whatsAppMessaging,
            OrderFlowEngine orderFlowEngine,
            OrderSessionManager sessionManager)
            : base(dbContext)
        {
            _orderFlowEngine = orderFlowEngine;
            _sessionManager = sessionManager;
            _whatsAppMessaging = whatsAppMessaging;
        }

        protected override async Task OnTextMessageReceived(WhatsAppMessageEvent messageEvent)
        {
            await _orderFlowEngine.ProcessMessage(
                messageEvent.BusinessId, 
                messageEvent.PhoneNumber, 
                messageEvent.Content, 
                messageEvent.CustomerName
            );
            
            await CleanupSessions();
        }

        protected override async Task OnInteractiveButtonClicked(WhatsAppMessageEvent messageEvent)
        {
            await _orderFlowEngine.ProcessMessage(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                messageEvent.InteractivePayload,
                messageEvent.CustomerName
            );
            
            await CleanupSessions();
        }

        protected override async Task OnInteractiveListSelected(WhatsAppMessageEvent messageEvent)
        {
            await _orderFlowEngine.ProcessMessage(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                messageEvent.InteractivePayload,
                messageEvent.CustomerName
            );
            
            await CleanupSessions();
        }

        protected override async Task OnOrderReceived(WhatsAppMessageEvent messageEvent)
        {
            await _orderFlowEngine.ProcessOrderMessage(
                messageEvent.BusinessId, 
                messageEvent.PhoneNumber, 
                messageEvent.RawMessage["order"]
            );
            
            await CleanupSessions();
        }

        protected override async Task OnUnknownMessageType(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "I'm sorry, I don't understand that type of message. Please send text or use the provided options."
            );
        }

        private async Task CleanupSessions()
        {
            await _sessionManager.CleanupOldSessions();
            await _sessionManager.CleanupCancelledSessions();
        }
    }
}