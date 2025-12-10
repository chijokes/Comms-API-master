using System;
using TimeZoneConverter;
using FusionComms.Entities.WhatsApp;


namespace FusionComms.Utilities
{
    public static class AppTimeZones
    {
        public static readonly TimeZoneInfo Nigeria =
            TZConvert.GetTimeZoneInfo("Africa/Lagos");
    }

    public static class WhatsAppTemplateConstants
    {
        public const string PaymentConfirmationTemplate = "payment_confirmation_template";
        public const string OrderPaymentTemplate = "order_payment_template";
        public const string OrderPaymentTemplateV2 = "order_payment_template_v2";
        public const string OrderPaymentTemplateV3 = "order_payment_template_v3";
    }

    public static class SearchMessages
    {
        public const string SEARCH_PROMPT = "🔍 *Search Menu*\n\nType what you're looking for (e.g., 'burger', 'pizza', 'chicken'):";
        public const string NO_SEARCH_RESULTS = "❌ No items found matching *{0}*.\n\nTry different keywords or browse the full menu.";
    }

    public static class TimeZoneHelper
    {
        private static readonly TimeSpan WatOffset = TimeSpan.FromHours(1);

        public static DateTime GetWatNow()
        {
            return DateTime.UtcNow + WatOffset;
        }

        public static DateTime ToWat(DateTime utcDateTime)
        {
            return utcDateTime + WatOffset;
        }
    }

    public static class MessageFormattingHelper
    {
        public static string FormatHelpContactFooter(OrderSession session)
        {
            var helpEmail = session.HelpEmail;
            var helpPhone = session.HelpPhoneNumber ?? "no-contact-phone";

            if (!string.IsNullOrWhiteSpace(helpEmail))
            {
                return $"\n\n📞 Need help? Call {helpPhone} or email {helpEmail}";
            }

            return $"\n\n📞 Need help? Call {helpPhone}";
        }
    }
}