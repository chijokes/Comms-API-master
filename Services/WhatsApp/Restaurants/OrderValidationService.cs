using FusionComms.Utilities;
using System;
using System.Linq;
using FusionComms.Entities.WhatsApp;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class OrderValidationService
    {
        public bool IsValidAddress(string address)
        {
            return !string.IsNullOrWhiteSpace(address) && address.Length >= 10;
        }

        public bool IsValidItemNumber(string message, int maxItems)
        {
            if (!int.TryParse(message, out int itemNumber))
                return false;
            
            return itemNumber >= 1 && itemNumber <= maxItems;
        }

        public bool IsTriggerWord(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var trimmedMessage = message.Trim().ToLower();

            return trimmedMessage == "hi" ||
                trimmedMessage == "hi!" ||
                trimmedMessage == "hello" ||
                trimmedMessage == "hey" ||
                trimmedMessage.StartsWith("hi ") ||
                trimmedMessage.StartsWith("hi! ") ||
                trimmedMessage.StartsWith("hello ") ||
                trimmedMessage.StartsWith("hey ");
        }

        public bool IsValidButtonCommand(string message)
        {
            var validCommands = new[] {
                "START_ORDER", "GET_HELP", "CONFIRM_ORDER", "EDIT_ORDER", "CANCEL_ORDER",
                "ADD_ITEM", "REMOVE_ITEM", "BACK_TO_SUMMARY", "APPLY_DISCOUNT",
                "PROFILE_EMAIL", "PROFILE_ADDRESSES", "PROFILE_ADD_EMAIL", "PROFILE_REMOVE_EMAIL",
                "PROFILE_ADD_ADDRESS", "PROFILE_REMOVE_ADDRESS", "PROFILE_BACK_TO_MENU",
                "PROFILE_CONTINUE_ORDER", "PROFILE_BACK_TO_MAIN",
                "REMOVE_EMAIL_YES", "REMOVE_EMAIL_NO",
                "PROFILE_PHONE", "PROFILE_ADD_PHONE", "PROFILE_REMOVE_PHONE",
                "CONFIRM_REMOVE_PHONE_YES", "CONFIRM_REMOVE_PHONE_NO",
                "SWITCH_TO_PICKUP_YES", "SWITCH_TO_PICKUP_NO"
            };
            
            var orderFlowCommands = new[] {
                "SAVE_EMAIL_YES", "SAVE_EMAIL_NO", "SAVE_ADDRESS_YES", "SAVE_ADDRESS_NO"
            };
            
            return validCommands.Contains(message, StringComparer.OrdinalIgnoreCase) || 
                orderFlowCommands.Contains(message, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsSearchRequest(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            var searchKeywords = new[] { "search", "find", "look for", "looking for" };
            var normalizedMessage = message.ToLower().Trim();
            
            return searchKeywords.Any(keyword => normalizedMessage.Contains(keyword)) ||
                normalizedMessage.Equals("🔍 search menu") ||
                normalizedMessage.Equals("search");
        }

        public bool IsHelpRequest(string message)
        {
            var helpKeywords = new[] { "help", "how", "why", "can", "support" };
            return helpKeywords.Contains(message.ToLower());
        }

        public bool IsCancelRequest(string message)
        {
            return (message.Contains("cancel", StringComparison.OrdinalIgnoreCase) || 
                    message.Equals("CANCEL_ORDER", StringComparison.OrdinalIgnoreCase)) && 
                !message.Equals("CONFIRM_CANCEL") && 
                !message.Equals("CONTINUE_ORDER");
        }

        public bool IsLocationOpen(DateTime startTime, DateTime endTime)
        {
            var nowWat = TimeZoneHelper.GetWatNow();

            var startTod = TimeZoneHelper.ToWat(startTime).TimeOfDay;
            var endTod = TimeZoneHelper.ToWat(endTime).TimeOfDay;

            var today = nowWat.Date;
            var start = today + startTod;
            var end = today + endTod;

            if (end <= start)
            {
                if (nowWat.TimeOfDay >= startTod)
                {
                    end = end.AddDays(1);
                }
                else
                {
                    start = start.AddDays(-1);
                }
            }

            return nowWat >= start && nowWat <= end;
        }

        public bool IsValidItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            
            return Guid.TryParse(itemId, out _);
        }

        public bool AllowsCatalogInteraction(string currentState)
        {
            var catalogAllowedStates = new[] {
                "ITEM_SELECTION",
                "ITEM_SELECTION_FROM_EDIT",
                "ITEM_SELECTION_FOR_TOPPINGS",
                "ITEM_OPTIONS",
                "ITEM_TOPPINGS",
                "EDIT_ORDER",
                "SEARCH",
                "SEARCH_RESULTS"
            };
            
            var result = catalogAllowedStates.Contains(currentState);
            return result;
        }

        public bool AllowsCheckout(string currentState)
        {
            var checkoutAllowedStates = new[] { 
                "ORDER_CONFIRMATION", 
                "COLLECT_NOTES" 
            };
            
            return checkoutAllowedStates.Contains(currentState);
        }

        public bool HasCompletedRequiredSteps(OrderSession session, string action)
        {
            var result = action switch
            {
                "ADD_TO_CART" => !string.IsNullOrEmpty(session.RevenueCenterId),
                "CHECKOUT" => !string.IsNullOrEmpty(session.RevenueCenterId) &&
                                            !string.IsNullOrEmpty(session.DeliveryMethod),
                "DELIVERY" => !string.IsNullOrEmpty(session.RevenueCenterId) &&
                                            !string.IsNullOrEmpty(session.DeliveryMethod) &&
                                            !string.IsNullOrEmpty(session.DeliveryAddress),
                "PLACE_ORDER" => !string.IsNullOrEmpty(session.RevenueCenterId) &&
                                        !string.IsNullOrEmpty(session.DeliveryMethod) &&
                                        ((!string.IsNullOrEmpty(session.DeliveryAddress) && !string.IsNullOrEmpty(session.DeliveryChargeId)) || session.DeliveryMethod == "Pickup"),
                _ => true,
            };
            
            return result;
        }

        public bool IsActionAllowedInState(string currentState, string action)
        {
            var packManagementStates = new[] { "PACK_SELECTION_ADD", "PACK_SELECTION_REMOVE" };
            
            var result = action switch
            {
                "LOCATION_SELECTION" => IsLocationSelectionAllowed(currentState),
                "ITEM_SELECTION" => IsItemSelectionAllowed(currentState) || packManagementStates.Contains(currentState),
                "DELIVERY_METHOD" => IsDeliveryMethodAllowed(currentState),
                "DELIVERY_ADDRESS" => IsDeliveryAddressAllowed(currentState),
                "COLLECT_NOTES" => IsNotesCollectionAllowed(currentState),
                "EMAIL_COLLECTION" => IsEmailCollectionAllowed(currentState),
                "ORDER_CONFIRMATION" => IsOrderConfirmationAllowed(currentState),
                "EDIT_ORDER" => IsEditOrderAllowed(currentState) || packManagementStates.Contains(currentState),
                "CANCEL_ORDER" => IsCancelOrderAllowed(currentState),
                "ADD_ITEM" => IsAddItemAllowed(currentState) || packManagementStates.Contains(currentState),
                "REMOVE_ITEM" => IsRemoveItemAllowed(currentState) || packManagementStates.Contains(currentState),
                "BACK_TO_SUMMARY" => IsBackToSummaryAllowed(currentState),
                "PROFILE_MANAGEMENT" => IsProfileManagementAllowed(currentState),
                "HELP" => IsHelpAllowed(currentState),
                "MENU_ACCESS" => IsMenuAccessAllowed(currentState),
                "SEARCH" => IsSearchAllowed(currentState),
                "FULL_MENU" => IsFullMenuAllowed(currentState),
                "SEARCH_RESULTS" => IsSearchResultsAllowed(currentState),
                "APPLY_DISCOUNT" => IsApplyDiscountAllowed(currentState),
                _ => true
            };
            
            return result;
        }

        private bool IsLocationSelectionAllowed(string currentState)
        {
            var allowedStates = new[] { "LOCATION_SELECTION" };
            return allowedStates.Contains(currentState);
        }

        private bool IsItemSelectionAllowed(string currentState)
        {
            var allowedStates = new[] { "ITEM_SELECTION", "ITEM_SELECTION_FROM_EDIT", "ITEM_SELECTION_FOR_TOPPINGS" };
            return allowedStates.Contains(currentState);
        }

        private bool IsSearchAllowed(string currentState)
        {
            var allowedStates = new[] { 
                "ITEM_SELECTION", 
                "ITEM_SELECTION_FROM_EDIT",
                "ITEM_SELECTION_FOR_TOPPINGS",
                "SEARCH",
                "SEARCH_RESULTS"
            };
            return allowedStates.Contains(currentState);
        }

        private bool IsFullMenuAllowed(string currentState)
        {
            var allowedStates = new[] { 
                "ITEM_SELECTION", 
                "ITEM_SELECTION_FROM_EDIT",
                "ITEM_SELECTION_FOR_TOPPINGS",
                "SEARCH",
                "SEARCH_RESULTS"
            };
            return allowedStates.Contains(currentState);
        }

        private bool IsSearchResultsAllowed(string currentState)
        {
            var allowedStates = new[] { "SEARCH", "SEARCH_RESULTS" };
            return allowedStates.Contains(currentState);
        }

        private bool IsDeliveryMethodAllowed(string currentState)
        {
            var allowedStates = new[] { "DELIVERY_METHOD" };
            return allowedStates.Contains(currentState);
        }

        private bool IsDeliveryAddressAllowed(string currentState)
        {
            var allowedStates = new[] { "DELIVERY_ADDRESS" };
            return allowedStates.Contains(currentState);
        }

        private bool IsNotesCollectionAllowed(string currentState)
        {
            var allowedStates = new[] { "COLLECT_NOTES" };
            return allowedStates.Contains(currentState);
        }

        private bool IsEmailCollectionAllowed(string currentState)
        {
            return false;
        }

        private bool IsOrderConfirmationAllowed(string currentState)
        {
            var allowedStates = new[] { "ORDER_CONFIRMATION", "WAITING_FOR_DISCOUNT_CODE" };
            return allowedStates.Contains(currentState);
        }

        private bool IsEditOrderAllowed(string currentState)
        {
            var allowedStates = new[] { "EDIT_ORDER", "ORDER_CONFIRMATION", "WAITING_FOR_DISCOUNT_CODE" };
            return allowedStates.Contains(currentState);
        }

        private bool IsAddItemAllowed(string currentState)
        {
            var allowedStates = new[] { "EDIT_ORDER", "ITEM_SELECTION", "ITEM_SELECTION_FROM_EDIT" };
            return allowedStates.Contains(currentState);
        }

        private bool IsRemoveItemAllowed(string currentState)
        {
            var allowedStates = new[] { "EDIT_ORDER", "ITEM_SELECTION", "ITEM_SELECTION_FROM_EDIT" };
            return allowedStates.Contains(currentState);
        }

        private bool IsBackToSummaryAllowed(string currentState)
        {
            var allowedStates = new[] { 
                "EDIT_ORDER", 
                "ITEM_SELECTION", 
                "ITEM_SELECTION_FROM_EDIT",
                "REMOVE_ITEM_PROMPT",
                "PACK_SELECTION_ADD",
                "PACK_SELECTION_REMOVE",
                "COLLECT_NOTES",
                "WAITING_FOR_DISCOUNT_CODE"
            };
            return allowedStates.Contains(currentState);
        }

        private bool IsCancelOrderAllowed(string currentState)
        {
            var restrictedStates = new[] { "ADDRESS_SAVE_PROMPT" };
            return !restrictedStates.Contains(currentState);
        }

        private bool IsProfileManagementAllowed(string currentState)
        {
            var restrictedStates = new[] {
                "COLLECT_NOTES",
                "ADDRESS_SAVE_PROMPT",
                "ORDER_CONFIRMATION",
                "ITEM_OPTIONS"
            };
            
            if (restrictedStates.Contains(currentState))
            {
                return false;
            }
            
            var allowedStates = new[] {
                "LOCATION_SELECTION",
                "ITEM_SELECTION",
                "ITEM_SELECTION_FROM_EDIT",
                "ITEM_SELECTION_FOR_TOPPINGS",
                "DELIVERY_METHOD",
                "DELIVERY_LOCATION_SELECTION",
                "DELIVERY_ADDRESS",
                "PROFILE_MANAGEMENT",
                "PROFILE_MENU",
                "PHONE_MENU",
                "ADDRESS_MENU",
                "SEARCH",
                "SEARCH_RESULTS"
            };
            
            return allowedStates.Contains(currentState);
        }

        public bool IsDiscountRequest(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;
            
            var normalized = message.ToLower().Trim();
            
            var discountTriggers = new[] { "discount", "dicount", "add discount", "apply discount", "code", "coupon", "token" };
            
            return discountTriggers.Any(trigger => normalized.Contains(trigger));
        }

        private bool IsHelpAllowed(string currentState)
        {
            var restrictedStates = new[] { 
                "ADDRESS_SAVE_PROMPT",
                "ITEM_OPTIONS"
            };
            return !restrictedStates.Contains(currentState);
        }

        private bool IsMenuAccessAllowed(string currentState)
        {
            var allowedStates = new[] { "ITEM_SELECTION", "ITEM_SELECTION_FROM_EDIT", "ITEM_SELECTION_FOR_TOPPINGS", "EDIT_ORDER" };
            return allowedStates.Contains(currentState);
        }

        private bool IsApplyDiscountAllowed(string currentState)
        {
            var allowedStates = new[] {
                "ITEM_SELECTION",
                "ORDER_CONFIRMATION",
                "EDIT_ORDER",
                "COLLECT_NOTES",
                "WAITING_FOR_DISCOUNT_CODE",
                "PACK_SELECTION_ADD",
                "PACK_SELECTION_REMOVE",
                "REMOVE_ITEM_PROMPT"
            };
            return allowedStates.Contains(currentState);
        }

        public string GetNextRequiredStep(OrderSession session)
        {
            if (string.IsNullOrEmpty(session.RevenueCenterId))
                return "LOCATION_SELECTION";

            if (string.IsNullOrEmpty(session.DeliveryMethod))
                return "DELIVERY_METHOD";

            if (session.DeliveryMethod == "Delivery" && string.IsNullOrEmpty(session.DeliveryChargeId))
                return "DELIVERY_LOCATION_SELECTION";

            if (session.DeliveryMethod == "Delivery" && string.IsNullOrEmpty(session.DeliveryAddress))
                return "DELIVERY_ADDRESS";

            if (session.CurrentState == "DELIVERY_CONTACT_PHONE")
                return "DELIVERY_CONTACT_PHONE";

            if (string.IsNullOrEmpty(session.Notes))
                return "COLLECT_NOTES";

            return "ORDER_CONFIRMATION";
        }

        public string GetNextStepMessage(OrderSession session)
        {
            var nextStep = GetNextRequiredStep(session);

            return nextStep switch
            {
                "LOCATION_SELECTION" => "📍 *Location Required*\n\nPlease select a location to continue with your order.",
                "DELIVERY_METHOD" => "🚚 *Delivery Method Required*\n\nPlease choose how you'd like to receive your order.",
                "DELIVERY_LOCATION_SELECTION" => "🏠 *Delivery Area Required*\n\nPlease select your delivery area.",
                "DELIVERY_ADDRESS" => "📝 *Delivery Address Required*\n\nPlease enter your delivery address.",
                "COLLECT_NOTES" => "📝 *Special Instructions*\n\nPlease add any special requests or notes for your order.",
                "ORDER_CONFIRMATION" => "✅ *Ready to Confirm*\n\nYour order is ready for confirmation.",
                _ => "Please complete the current step to continue."
            };
        }
    }
}
