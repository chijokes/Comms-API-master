namespace FusionComms.Utilities
{
    public static class MessageConstants
    {
        public const string DeliveryContactPhonePrompt = "📱 Please enter a contact phone number for delivery: (e.g., +2348012345678, 08012345678)";

        public const string SpecialInstructionsPrompt = "📝 *Special Instructions*\n\n" +
            "Got any order requests/notes?\n(e.g., gate pass, no pork, extra spicy)\n\n" +
            "Click 'none' to skip.";

        public const string CatalogFetchFailed = "❌ Catalog Fetch Failed\n\nWe're unable to load our menu at the moment. Please try again later or contact support if the issue persists.{0}";
    }
}