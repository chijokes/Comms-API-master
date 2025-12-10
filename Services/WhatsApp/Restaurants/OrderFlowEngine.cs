using FusionComms.DTOs.WhatsApp;
using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using FusionComms.Utilities;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class OrderFlowEngine
    {
        private readonly OrderService _orderService;
        private readonly WhatsAppMessagingService _messagingService;
        private readonly AppDbContext _db;
        private readonly OrderSessionManager _sessionManager;
        private readonly OrderStateManager _stateManager;
        private readonly OrderUIManager _uiManager;
        private readonly OrderCartManager _cartManager;
        private readonly OrderValidationService _validationService;
        private readonly ProfileManager _profileManager;
        private readonly ProductSearchService _searchService;

        public OrderFlowEngine(
            OrderService orderService, 
            WhatsAppMessagingService messagingService, 
            AppDbContext dbContext,
            OrderSessionManager sessionManager,
            OrderStateManager stateManager,
            OrderUIManager uiManager,
            OrderCartManager cartManager,
            OrderValidationService validationService,
            ProfileManager profileManager,
            ProductSearchService searchService)
        {
            _orderService = orderService;
            _messagingService = messagingService;
            _db = dbContext;
            _sessionManager = sessionManager;
            _uiManager = uiManager;
            _cartManager = cartManager;
            _validationService = validationService;
            _stateManager = stateManager;
            _profileManager = profileManager;
            _searchService = searchService;
        }

        public async Task ProcessMessage(string businessId, string phoneNumber, string message, string customerName = null)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);

            if (session != null && IsMismatchedButtonClick(session, message))
            {
                await HandleMismatchedButtonClick(session, message);
                return;
            }
            
            var isTriggerWord = _validationService.IsTriggerWord(message);
            var isValidButtonCommand = _validationService.IsValidButtonCommand(message);

            if (session == null)
            {
                var business = await _db.WhatsAppBusinesses.FindAsync(businessId);
                if (business == null || !business.SupportsChat)
                {
                    await _uiManager.SendPersonalizedGreeting(businessId, phoneNumber, customerName, "Service Unavailable");
                    return;
                }

                var newSession = await _sessionManager.GetOrCreateSession(businessId, phoneNumber);
                await _sessionManager.UpdateCustomerName(newSession, customerName);
                
                newSession.CurrentState = "LOCATION_SELECTION";
                await _sessionManager.UpdateSessionState(newSession, "LOCATION_SELECTION");
                
                await _uiManager.ShowLocationSelection(newSession, newSession.CustomerName); 
                return;
            }

            await _sessionManager.UpdateCustomerName(session, customerName);
            
            if (isTriggerWord)
            {
                await ResetSessionForRestart(session);
                return;
            }
            
            if (session.CurrentState == "CANCELLED")
            {
                return;
            }
            
            var cart = _cartManager.DeserializeCart(session.CartData);
            
            if (!string.IsNullOrEmpty(session.ProfileState))
            {
                if (session.ProfileState == "PROFILE_MENU")
                {
                    await HandleProfileMenuInput(session, message);
                    return;
                }
                
                if (session.ProfileState == "ADDRESS_MENU")
                {
                    await HandleAddressMenuInput(session, message);
                    return;
                }
                
                if (session.ProfileState.StartsWith("WAITING_FOR_"))
                {
                    if (IsNavigationCommand(message))
                    {
                        await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                        return;
                    }
                    
                    await HandleProfileStateInput(session, message);
                    return;
                }
                
                await HandleProfileStateInput(session, message);
                return;
            }
            
            var saveRejectButtons = new[] { 
                "SAVE_ADDRESS_YES", "SAVE_ADDRESS_NO",
                "✅ Yes, save it", "👍 No, thanks"
            };
            
            if (saveRejectButtons.Contains(message))
            {
            }
            else if (message.Equals("manage profile", StringComparison.OrdinalIgnoreCase) || IsProfileManagementButton(message))
            {
                if (!_validationService.IsActionAllowedInState(session.CurrentState, "PROFILE_MANAGEMENT"))
                {
                    await SendProfileManagementBlockedMessage(session);
                    return;
                }
                
                if (IsProfileManagementButton(message))
                {
                    if (message.StartsWith("ADDRESS_") && message != "ADDRESS_SAVE_PROMPT")
                    {
                        await HandleAddressSelection(session, message);
                        return;
                    }
                    
                    await HandleProfileManagementWithSession(session, message);
                    return;
                }
            }
            
            if (string.IsNullOrEmpty(session.ProfileState) && IsProfileManagementAllowed(session.CurrentState, message))
            {
                await HandleProfileManagementWithSession(session, message);
                return;
            }
            
            if (session.CurrentState.StartsWith("PROFILE_"))
            {
                await HandleProfileCurrentState(session, message);
                return;
            }

            if (message.StartsWith("ADDRESS_") || message.StartsWith("Address ") || message == "ADD_NEW_ADDRESS")
            {
                await HandleAddressSelection(session, message);
                return;
            }

            if (_validationService.IsDiscountRequest(message) && session.CurrentState != "WAITING_FOR_DISCOUNT_CODE")
            {
                if (_validationService.IsActionAllowedInState(session.CurrentState, "APPLY_DISCOUNT"))
                {
                    await _stateManager.HandleDiscountCodeRequest(session);
                    session.LastInteraction = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return;
                }
            }

            if (message.Equals("APPLY_DISCOUNT", StringComparison.OrdinalIgnoreCase))
            {
                if (_validationService.IsActionAllowedInState(session.CurrentState, "APPLY_DISCOUNT"))
                {
                    await _stateManager.HandleDiscountCodeRequest(session);
                    session.LastInteraction = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return;
                }
                else
                {
                    await HandleActionNotAllowedInState(session, "APPLY_DISCOUNT");
                    return;
                }
            }

            if (isValidButtonCommand)
            {
                var action = ExtractActionFromButton(message);
                if (!_validationService.IsActionAllowedInState(session.CurrentState, action))
                {
                    await HandleActionNotAllowedInState(session, action);
                    return;
                }
            }

            if (message.Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                if (!_validationService.IsActionAllowedInState(session.CurrentState, "MENU_ACCESS"))
                {
                    await HandleInvalidMenuAccess(session);
                    return;
                }

                if (!_validationService.HasCompletedRequiredSteps(session, "ADD_TO_CART"))
                {
                    await HandleIncompleteFlow(session, "ADD_TO_CART");
                    return;
                }

                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "📋 The menu is already displayed below.\n\n" +
                    "Use the catalog to browse items and add them to your cart.");
                return;
            }
            
            if (message.Equals("manage profile", StringComparison.OrdinalIgnoreCase))
            {
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                return;
            }

            if (_validationService.IsHelpRequest(message))
            {
                if (!_validationService.IsActionAllowedInState(session.CurrentState, "HELP"))
                {
                    await HandleHelpNotAllowedInState(session);
                    return;
                }
                
                await HandleHelpRequest(session);
                return;
            }

            if (_validationService.IsSearchRequest(message))
            {
                await HandleSearchRequest(session);
                return;
            }

            if (_validationService.IsCancelRequest(message))
            {
                if (!_validationService.IsActionAllowedInState(session.CurrentState, "CANCEL_ORDER"))
                {
                    await HandleCancelNotAllowedInState(session);
                    return;
                }
                
                var sessionCart = _cartManager.DeserializeCart(session.CartData);
                if (!sessionCart.Items.Any() && string.IsNullOrEmpty(session.RevenueCenterId))
                {
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ No active order to cancel.");
                    session.CurrentState = "LOCATION_SELECTION";
                    await _db.SaveChangesAsync();
                    return;
                }
                
                await _stateManager.HandleCancelConfirmation(session);
                return;
            }

            if (isValidButtonCommand)
            {
                await ProcessMessageByState(session, cart, message, isTriggerWord, isValidButtonCommand);
                return;
            }

            await ProcessMessageByState(session, cart, message, isTriggerWord, isValidButtonCommand);

            session.LastInteraction = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ProcessOrderMessage(string businessId, string phoneNumber, JToken orderData)
        {
            try
            {
                var productItems = orderData["product_items"]?.ToObject<List<WhatsAppOrderItem>>();
                if (productItems == null || !productItems.Any())
                {
                    return;
                }

                var formattedPhoneNumber = OrderSessionManager.FormatWhatsAppPhoneNumber(phoneNumber);
                
                var existingSession = await _sessionManager.GetExistingSession(businessId, phoneNumber);
                
                OrderSession session;
                OrderCart cart;
                
                if (existingSession == null)
                {
                    session = await _sessionManager.GetOrCreateSession(businessId, phoneNumber);
                    cart = _cartManager.DeserializeCart(session.CartData);
                }
                else
                {
                    session = existingSession;
                    cart = _cartManager.DeserializeCart(session.CartData);
                }

                if (!_validationService.AllowsCatalogInteraction(session.CurrentState))
                {
                    await HandleInvalidCatalogInteraction(session, "ADD_TO_CART");
                    return;
                }

                if (!_validationService.HasCompletedRequiredSteps(session, "ADD_TO_CART"))
                {
                    await HandleIncompleteFlow(session, "ADD_TO_CART");
                    return;
                }

                var business = await _db.WhatsAppBusinesses.FindAsync(businessId);
                if (business == null)
                {
                    return;
                }

                var itemIds = productItems.Select(item => item.Product_retailer_id).ToList();
                var catalogItems = await _orderService.GetItemsAsync(business.RestaurantId, session.RevenueCenterId, itemIds);
                var pendingParents = JsonConvert.DeserializeObject<List<PendingParent>>(session.PendingParents ?? "[]");
                
                var pendingToppingsList = JsonConvert.DeserializeObject<List<PendingToppings>>(session.PendingToppingsQueue ?? "[]");
                bool toppingsWereAdded = false;

                foreach (var item in productItems)
                {
                    var catalogItem = catalogItems.FirstOrDefault(i => i.Id == item.Product_retailer_id);

                    if (catalogItem == null)
                    {
                        continue;
                    }
                    
                    bool hasToppings = !string.IsNullOrEmpty(catalogItem?.ToppingClassId);

                    if (catalogItem.HasRecipeParents && catalogItem.RecipeParents.Any())
                    {
                        var recipeParent = catalogItem.RecipeParents.First();
                        if (recipeParent.ItemParent?.ItemsInParent?.Any() != true) continue;

                        for (int i = 0; i < item.Quantity; i++)
                        {
                            string groupingId = Guid.NewGuid().ToString();

                            pendingParents.Add(new PendingParent
                            {
                                ParentItem = catalogItem,
                                RecipeParent = recipeParent,
                                Quantity = 1,
                                CurrentOptionIndex = 1,
                                OptionSetIndex = pendingParents.Count + 1,
                                TotalOptionSets = item.Quantity,
                                GroupingId = groupingId,
                                HasToppings = hasToppings,
                                ToppingClassId = catalogItem.ToppingClassId
                            });

                            _cartManager.AddItemToCart(cart, new CartItem
                            {
                                ItemId = catalogItem.Id,
                                Name = catalogItem.Name,
                                Price = item.Item_price,
                                ItemClassId = catalogItem.ItemClassId,
                                TaxId = catalogItem.TaxId,
                                IsParentItem = true,
                                GroupingId = groupingId,
                                Quantity = 1,
                                PackId = session.CurrentPackId ?? "pack1"
                            });
                        }
                    }
                    else if (hasToppings)
                    {
                        string groupingId = Guid.NewGuid().ToString();

                        _cartManager.AddItemToCart(cart, new CartItem
                        {
                            ItemId = catalogItem.Id,
                            Name = catalogItem.Name,
                            Price = item.Item_price,
                            ItemClassId = catalogItem.ItemClassId,
                            TaxId = catalogItem.TaxId,
                            Quantity = item.Quantity,
                            GroupingId = groupingId,
                            ParentItemId = null,
                            PackId = session.CurrentPackId ?? "pack1",
                            IsTopping = false
                        });

                        var toppings = await _orderService.GetToppingsAsync(catalogItem.ToppingClassId, session.RevenueCenterId);
                        
                        var pendingToppings = new PendingToppings
                        {
                            MainItemId = catalogItem.Id,
                            MainItemName = catalogItem.Name,
                            GroupingId = groupingId,
                            Toppings = toppings ?? new List<ToppingItem>()
                        };

                        pendingToppingsList.Add(pendingToppings);
                        toppingsWereAdded = true;
                    }
                    else
                    {
                        _cartManager.AddItemToCart(cart, new CartItem
                        {
                            ItemId = catalogItem.Id,
                            Name = catalogItem.Name,
                            Price = item.Item_price,
                            Quantity = item.Quantity,
                            ItemClassId = catalogItem.ItemClassId,
                            TaxId = catalogItem.TaxId,
                            GroupingId = null,
                            ParentItemId = null,
                            PackId = session.CurrentPackId ?? "pack1"
                        });
                    }
                }

                session.CartData = _cartManager.SerializeCart(cart);
                session.PendingParents = JsonConvert.SerializeObject(pendingParents);
                session.PendingToppingsQueue = JsonConvert.SerializeObject(pendingToppingsList);
                await _db.SaveChangesAsync();

                if (pendingParents.Any())
                {
                    session.CurrentState = "ITEM_OPTIONS";
                    await _db.SaveChangesAsync();
                    await _uiManager.ShowItemOptions(session.BusinessId, session.PhoneNumber, pendingParents.First());
                }
                else if (toppingsWereAdded && pendingToppingsList.Any())
                {
                    session.CurrentState = "ITEM_TOPPINGS";
                    await _db.SaveChangesAsync();
                    await _uiManager.ShowToppingsSelection(session.BusinessId, session.PhoneNumber, pendingToppingsList.First());
                }
                else
                {
                    var smallCatalog = session.CurrentMenuLevel == "products_small";
                    if (smallCatalog)
                    {
                        if (string.IsNullOrEmpty(session.DeliveryMethod))
                        {
                            session.CurrentState = session.CurrentState == "ITEM_SELECTION_FROM_EDIT" ? "ITEM_SELECTION_FROM_EDIT" : "DELIVERY_METHOD";
                            await _db.SaveChangesAsync();
                            await _uiManager.AskDeliveryMethod(session);
                        }
                        else
                        {
                            await _stateManager.ProceedToCheckoutFlow(session);
                        }
                    }
                    else
                    {
                        session.CurrentState = session.CurrentState == "ITEM_SELECTION_FROM_EDIT" ? "ITEM_SELECTION_FROM_EDIT" : "ITEM_SELECTION";
                        await _db.SaveChangesAsync();
                        await _uiManager.ShowPostAddOptions(session);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task ProcessMessageByState(OrderSession session, OrderCart cart, string message, bool isTriggerWord, bool isValidButtonCommand)
        {
            if (message.StartsWith("REMOVE_PACK_") || message.StartsWith("ADD_PACK_") || 
                message == "ADD_NEW_PACK" || message.EndsWith("_NEW_PACK"))
            {
                var action = message.StartsWith("REMOVE_PACK_") ? "REMOVE" : "ADD";
                await _stateManager.HandlePackSelection(session, cart, message, action);
                return;
            }
            switch (session.CurrentState)
            {
                case "LOCATION_SELECTION":
                    if (isTriggerWord)
                    {
                        await ResetSessionForRestart(session);
                        return;
                    }
                    if (isValidButtonCommand)
                    {
                        await _stateManager.HandleLocationSelection(session, message);
                        return;
                    }
                    
                    await _stateManager.HandleLocationSelection(session, message);
                    break;
                case "ITEM_SELECTION":
                case "ITEM_SELECTION_FROM_EDIT":
                    await _stateManager.HandleItemSelection(session, cart, message);
                    break;
                case "ITEM_OPTIONS":
                    await _stateManager.HandleItemOptions(session, cart, message);
                    break;
                case "ITEM_TOPPINGS":
                    await _stateManager.HandleItemToppings(session, cart, message);
                    break;
                case "ORDER_CONFIRMATION":
                    await _stateManager.HandleOrderConfirmation(session, cart, message);
                    break;
                case "WAITING_FOR_DISCOUNT_CODE":
                    await _stateManager.HandleDiscountCodeEntry(session, cart, message);
                    break;
                case "DELIVERY_METHOD":
                    await _stateManager.HandleDeliveryMethod(session, cart, message);
                    break;
                case "DELIVERY_LOCATION_SELECTION":
                    await _stateManager.HandleDeliveryLocationSelection(session, cart, message);
                    break;
                case "DELIVERY_CONTACT_PHONE":
                    await _stateManager.HandleDeliveryContactPhone(session, cart, message);
                    break;
                case "DELIVERY_SWITCH_CONFIRMATION":
                    await _stateManager.HandleDeliverySwitchConfirmation(session, cart, message);
                    break;
                case "DELIVERY_ADDRESS":
                    await _stateManager.HandleDeliveryAddress(session, cart, message);
                    break;
                case "EDIT_ORDER":
                    await _stateManager.HandleEditOrder(session, cart, message);
                    break;
                case "PACK_SELECTION_ADD":
                case "PACK_SELECTION_REMOVE":
                    await _stateManager.HandlePackSelection(session, cart, message, 
                        session.CurrentState == "PACK_SELECTION_ADD" ? "ADD" : "REMOVE");
                    break;
                case "REMOVE_ITEM_PROMPT":
                    await _stateManager.HandleRemoveItemByNumber(session, cart, message);
                    break;
                case "CANCEL_CONFIRMATION":
                    await _stateManager.HandleCancelConfirmationResponse(session, cart, message);
                    break;
                case "COLLECT_NOTES":
                    await _stateManager.HandleNotesCollection(session, cart, message);
                    break;
                case "ADDRESS_SAVE_PROMPT":
                    await HandleAddressSavePrompt(session, cart, message);
                    break;
                case "SEARCH":
                    await HandleSearchState(session, message);
                    break;
                case "CONFIRM_CLOSED_RESTAURANT":
                    await _stateManager.HandleClosedRestaurantConfirmation(session, message);
                    break;
                case "CONFIRM_CLOSED_DELIVERY":
                    await _stateManager.HandleClosedDeliveryConfirmation(session, message);
                    break;
                case "SEARCH_RESULTS":
                    await _stateManager.HandleItemSelection(session, cart, message);
                    break;
                    
                default:
                    await HandleDefaultState(session, message, isTriggerWord);
                    break;
            }
        }

        private async Task HandleDefaultState(OrderSession session, string message, bool isTriggerWord)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            
            if (isTriggerWord)
            {
                await SendDefaultTriggerResponse(session);
                return;
            }
            
            var statesExpectingInput = new[] { 
                "LOCATION_SELECTION",
                "ITEM_OPTIONS", "ORDER_CONFIRMATION", 
                "DELIVERY_METHOD", "DELIVERY_CONTACT_PHONE", "DELIVERY_LOCATION_SELECTION", "DELIVERY_ADDRESS", 
                "EDIT_ORDER", "REMOVE_ITEM_PROMPT", "CANCEL_CONFIRMATION", "COLLECT_NOTES",
                "ADDRESS_SAVE_PROMPT",
            };
            
            if (statesExpectingInput.Contains(session.CurrentState))
            {
                await SendGenericPrompt(session);
            }
        }

        private async Task HandleSearchState(OrderSession session, string message)
        {
            if (IsNavigationCommand(message) || message.Equals("FULL_MENU", StringComparison.OrdinalIgnoreCase) ||
                message.Equals("📖 Browse Menu", StringComparison.OrdinalIgnoreCase))
            {
                session.CurrentState = "ITEM_SELECTION";
                await _uiManager.ShowCategoriesList(session);
                return;
            }

            if (message.Equals("SEARCH", StringComparison.OrdinalIgnoreCase) || 
                message.Equals("🔍 Search Menu", StringComparison.OrdinalIgnoreCase))
            {
                await _uiManager.ShowSearchPrompt(session.BusinessId, session.PhoneNumber);
                session.CurrentState = "SEARCH";
                return;
            }

            if (_searchService.IsSearchQuery(message))
            {
                await ProcessSearchQuery(session, message);
                return;
            }

            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                "❌ Please enter a valid search term (2-50 characters) or use the buttons below." +
                MessageFormattingHelper.FormatHelpContactFooter(session));

            await _uiManager.ShowSearchActionButtons(session.BusinessId, session.PhoneNumber, session);
        }

        private async Task HandleSearchRequest(OrderSession session)
        {
            if (!_validationService.IsActionAllowedInState(session.CurrentState, "SEARCH"))
            {
                await HandleActionNotAllowedInState(session, "SEARCH");
                return;
            }

            session.CurrentState = "SEARCH";
            await _uiManager.ShowSearchPrompt(session.BusinessId, session.PhoneNumber);
        }

        private async Task ProcessSearchQuery(OrderSession session, string searchQuery)
        {
            var results = await _searchService.SearchProductsAsync(
                session.BusinessId, 
                session.RevenueCenterId, 
                searchQuery);

            session.CurrentState = "SEARCH_RESULTS";
            await _uiManager.ShowSearchResults(session.BusinessId, session.PhoneNumber, results, searchQuery);
        }

        private bool IsNavigationCommand(string message)
        {
            var navigationCommands = new[] { 
                "BACK_TO_MAIN", "BACK_CATEGORIES", "BACK_SUBCATEGORIES", 
                "VIEW_MORE_CATEGORIES", "BROWSE_OTHERS", "ADD_MORE" 
            };
            return navigationCommands.Contains(message, StringComparer.OrdinalIgnoreCase);
        }

        private async Task ResetSessionForRestart(OrderSession session)
        {
            session.CurrentState = "LOCATION_SELECTION";
            session.CartData = JsonConvert.SerializeObject(new OrderCart());
            session.PendingParents = "[]";
            session.RevenueCenterId = null;
            session.DeliveryMethod = null;
            session.DeliveryAddress = null;
            session.DeliveryChargeId = null;
            session.Email = null;
            session.Notes = null;
            session.IsEditing = false;
            session.EditingGroupId = null;
            session.CurrentMenuLevel = null;
            session.CurrentCategoryGroup = null;
            session.CurrentSubcategoryGroup = null;
            session.ProfileState = null;
            session.EditGroupsData = null;
            session.CurrentPackId = null;
            session.PendingToppings = null;
            session.PendingToppingsQueue = "[]";
            session.LastInteraction = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _uiManager.ShowLocationSelection(session, session.CustomerName);
            await _db.SaveChangesAsync();
        }

        private async Task SendDefaultTriggerResponse(OrderSession session)
        {
            await ResetSessionForRestart(session);
        }

        private async Task SendGenericPrompt(OrderSession session)
        {
            if (session.CurrentState == "COLLECT_NOTES")
            {
                await _stateManager.SendSpecialInstructionsPrompt(session);
                return;
            }
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                "❓ I didn't understand that.\n\n" +
                "Please use the options below or the catalog menu.\n\n" +
                "Type 'manage profile' to manage your profile during this order.");
        }


        private async Task HandleHelpRequest(OrderSession session)
        {
            var helpEmail = session.HelpEmail ?? "No Support Email ";
            var helpPhone = session.HelpPhoneNumber ?? "No Support Phone";
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                "🛟 *Help & Support*\n\n" +
                "👤 Profile: Type 'manage profile' (during active order)\n" +
                "🔁 Restart: Type 'hi'\n" +
                "❌ Cancel: Type 'cancel'\n\n" +
                $"📞 To call support: {helpPhone}\n" +
                $"📧 To email support: {helpEmail}");
        }

        private async Task HandleInvalidCatalogInteraction(OrderSession session, string action)
        {
            var currentState = session.CurrentState;
            string message;

            switch (currentState)
            {

                case "LOCATION_SELECTION":
                  session.CurrentState = "LOCATION_SELECTION";
                    await _db.SaveChangesAsync();
                    await _uiManager.ShowLocationSelection(session, session.CustomerName);
                    return;
                case "DELIVERY_METHOD":
                    message = "🚚 *Delivery Method Required*\n\n" +
                            "Please select how you'd like to receive your order.\n\n" +
                            "To Add item(s) Complete process first.\n\n" +
                            "Choose from the delivery options below.";
                    break;
                case "DELIVERY_LOCATION_SELECTION":
                    message = "🏠 *Delivery Area Required*\n\n" +
                            "Please select your delivery area.\n\n" +
                            "To Add item(s) Complete process first.\n\n" +
                            "Choose from the delivery areas below.";
                    break;
                case "DELIVERY_ADDRESS":
                    message = "📝 *Delivery Address Required*\n\n" +
                            "Please enter your delivery address.\n\n" +
                            "To Add item(s) Complete process first.\n\n" +
                            "Type your complete address to continue.";
                    break;
                case "ORDER_CONFIRMATION":
                case "COLLECT_NOTES":
                    message = "*Order Ready for Review*\n\n" +
                            "Please review order and confirm.\nTo Add item(s) click 'Edit Order below";
                    break;
                default:
                    message = "🔄 *Flow Interrupted*\n\n" +
                            "It looks like you're trying to add items, but we need to get you back on track.\n\n" +
                            "Please type 'cancel' to restart your order.";
                    break;
            }

            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message + MessageFormattingHelper.FormatHelpContactFooter(session));

            if (currentState == "LOCATION_SELECTION")
            {
                await _uiManager.ShowLocationSelection(session);
            }
            else if (currentState == "DELIVERY_METHOD")
            {
                await _uiManager.AskDeliveryMethod(session);
            }
            else if (currentState == "DELIVERY_LOCATION_SELECTION")
            {
                await _uiManager.ShowDeliveryLocationSelection(session);
            }
            else if (currentState == "ORDER_CONFIRMATION")
            {
                var cart = _cartManager.DeserializeCart(session.CartData);
                await _uiManager.ShowOrderSummary(session, cart);
            }
        }

        private async Task HandleInvalidMenuAccess(OrderSession session)
        {
            var currentState = session.CurrentState;
            string message;

            switch (currentState)
            {
                case "LOCATION_SELECTION":
                    session.CurrentState = "LOCATION_SELECTION";
                    await _db.SaveChangesAsync();
                    await _uiManager.ShowLocationSelection(session, session.CustomerName);
                    return;
                default:
                    message = "🔄 *Flow Interrupted*\n\n" +
                            "It looks like you're trying to browse our menu, but we need to get you back on track.\n\n" +
                            "Please type 'Hi' to restart your order.";
                    break;
            }

            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message + MessageFormattingHelper.FormatHelpContactFooter(session));

            if (currentState == "LOCATION_SELECTION")
            {
                await _uiManager.ShowLocationSelection(session);
            }
        }

        private async Task HandleIncompleteFlow(OrderSession session, string action)
        {
            string message;

            switch (action)
            {
                case "ADD_TO_CART":
                    if (string.IsNullOrEmpty(session.RevenueCenterId))
                    {
                        message = "📍 *Location Required*\n\n" +
                                "You need to select a location first before adding items to your cart.\n\n" +
                                "Please choose a location to continue.";
                        
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            message + MessageFormattingHelper.FormatHelpContactFooter(session));
                        
                        session.CurrentState = "LOCATION_SELECTION";
                        await _uiManager.ShowLocationSelection(session);
                    }
                    break;

                case "CHECKOUT":
                    if (string.IsNullOrEmpty(session.DeliveryMethod))
                    {
                        message = "🚚 *Delivery Method Required*\n\n" +
                                "Please select how you'd like to receive your order.\n\n" +
                                "Choose from the delivery options below.";
                        
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            message + MessageFormattingHelper.FormatHelpContactFooter(session));
                        
                        session.CurrentState = "DELIVERY_METHOD";
                        await _uiManager.AskDeliveryMethod(session);
                    }
                    break;

                case "DELIVERY":
                    if (string.IsNullOrEmpty(session.DeliveryAddress))
                    {
                        message = "📝 *Delivery Address Required*\n\n" +
                                "Please enter your delivery address to continue.\n\n" +
                                "Type your complete address.";
                        
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            message + MessageFormattingHelper.FormatHelpContactFooter(session));
                        
                        session.CurrentState = "DELIVERY_ADDRESS";
                    }
                    break;

                default:
                    message = "🔄 *Flow Incomplete*\n\n" +
                            "Please complete the current step before proceeding.\n\n" +
                            "Follow the instructions above to continue.";
                    
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        message + MessageFormattingHelper.FormatHelpContactFooter(session));
                    break;
            }

            await _db.SaveChangesAsync();
        }


        private bool IsProfileManagementButton(string message)
        {
            var searchButtons = new[] { "SEARCH", "FULL_MENU", "🔍 Search Menu", "📖 Browse Menu" };
            if (searchButtons.Contains(message))
                return false;

            var profileButtons = new[] { 
                "PROFILE_ADDRESSES", "PROFILE_PHONE",
                "PROFILE_ADD_ADDRESS", "PROFILE_REMOVE_ADDRESS", 
                "PROFILE_ADD_PHONE", "PROFILE_REMOVE_PHONE",
                "CONFIRM_REMOVE_PHONE_YES", "CONFIRM_REMOVE_PHONE_NO",
                "PROFILE_BACK_TO_MENU", 
                "PROFILE_CONTINUE_ORDER", "PROFILE_BACK_TO_MAIN",
                "SAVE_ADDRESS_YES", "SAVE_ADDRESS_NO"
            };
            
            var profileButtonTexts = new[] {
                "🏠 Manage Addresses", "📞 Manage Contact Phone",
                "➕ Add New Address", "➕ Add Address", "🗑️ Remove Address",
                "📞 Add Contact Phone", "❌ Remove Contact Phone",
                "✅ Yes, Remove", "❎ No, Keep",
                "⬅️ Back", "🛒 Continue Order", "🏠 Main Menu",
                "✅ Yes, save it", "👍 No, thanks"
            };
            
            var isProfileButton = profileButtons.Any(button => 
                message.Equals(button, StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith(button, StringComparison.OrdinalIgnoreCase)) ||
                profileButtonTexts.Any(buttonText => 
                    message.Equals(buttonText, StringComparison.OrdinalIgnoreCase));
            
            if (!isProfileButton)
            {
                var normalizedMessage = message.Trim().ToLower();
                var hasYes = normalizedMessage.Contains("yes") || normalizedMessage.Contains("✅");
                var hasNo = normalizedMessage.Contains("no") || normalizedMessage.Contains("❌");
                var hasRemove = normalizedMessage.Contains("remove");
                var hasKeep = normalizedMessage.Contains("keep");
                var hasSave = normalizedMessage.Contains("save");
                var hasThanks = normalizedMessage.Contains("thanks");
                
                if ((hasYes && hasRemove) || (hasNo && hasKeep) || (hasYes && hasSave) || (hasNo && hasThanks))
                {
                    isProfileButton = true;
                }
            }
            
            return isProfileButton;
        }

        private bool IsProfileManagementAllowed(string currentState, string message)
        {
            var profileButtons = new[] { 
                "PROFILE_ADDRESSES", "PROFILE_PHONE",
                "PROFILE_ADD_ADDRESS", "PROFILE_REMOVE_ADDRESS", 
                "PROFILE_ADD_PHONE", "PROFILE_REMOVE_PHONE",
                "CONFIRM_REMOVE_PHONE_YES", "CONFIRM_REMOVE_PHONE_NO",
                "PROFILE_BACK_TO_MENU", "PROFILE_CONTINUE_ORDER", "PROFILE_BACK_TO_MAIN",
                "SAVE_ADDRESS_YES", "SAVE_ADDRESS_NO"
            };
            
            var profileButtonTexts = new[] {
                "🏠 Manage Addresses", "📞 Manage Contact Phone",
                "➕ Add New Address", "➕ Add Address", "🗑️ Remove Address",
                "📞 Add Contact Phone", "❌ Remove Contact Phone",
                "✅ Yes, Remove", "❎ No, Keep",
                "⬅️ Back", "🛒 Continue Order", "🏠 Main Menu", "📱 Add Phone",
                "✅ Yes, save it", "👍 No, thanks"
            };

            var isProfileButton = profileButtons.Any(button => 
                message.Equals(button, StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith(button, StringComparison.OrdinalIgnoreCase)) ||
                profileButtonTexts.Any(buttonText => 
                    message.Equals(buttonText, StringComparison.OrdinalIgnoreCase));
            
            if (!isProfileButton)
                return false;
            
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
                "DELIVERY_METHOD",
                "DELIVERY_LOCATION_SELECTION",
                "DELIVERY_ADDRESS",
                "PROFILE_MANAGEMENT",
                "PROFILE_MENU",
                "PHONE_MENU",
                "ADDRESS_MENU",
                "CONFIRM_PHONE_REMOVAL",
                "WAITING_FOR_PHONE",
                "WAITING_FOR_ADDRESS",
                "WAITING_FOR_ADDRESS_REMOVAL"
            };
            
            return allowedStates.Contains(currentState);
        }

        private async Task HandleProfileStateInput(OrderSession session, string message)
        {
            if (IsNavigationCommand(message))
            {
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                return;
            }

            var profileButtonPayloads = new[] {
                "PROFILE_ADDRESSES", "PROFILE_PHONE", 
                "PROFILE_ADD_ADDRESS", "PROFILE_REMOVE_ADDRESS", 
                "PROFILE_ADD_PHONE", "PROFILE_REMOVE_PHONE",
                "PROFILE_BACK_TO_MENU", "PROFILE_CONTINUE_ORDER", "PROFILE_BACK_TO_MAIN"
            };
            
            if (profileButtonPayloads.Contains(message))
            {
                await HandleProfileManagementWithSession(session, message);
                return;
            }
            
            switch (session.ProfileState)
            {
                case "PROFILE_MENU":
                    await HandleProfileMenuInput(session, message);
                    break;
                case "ADDRESS_MENU":
                    await HandleAddressMenuInput(session, message);
                    break;
                case "PHONE_MENU":
                    await HandlePhoneMenuInput(session, message);
                    break;
                case "CONFIRM_PHONE_REMOVAL":
                    await HandlePhoneConfirmationInput(session, message);
                    break;
                case "WAITING_FOR_ADDRESS":
                    await HandleProfileAddressInput(session, message);
                    break;
                case "WAITING_FOR_ADDRESS_REMOVAL":
                    await HandleProfileAddressRemovalInput(session, message);
                    break;
                case "WAITING_FOR_PHONE":
                    var phoneSaved = await _profileManager.ProcessPhoneInputAsync(session.BusinessId, session.PhoneNumber, message);
                    break;
                default:
                    session.ProfileState = null;
                    await _db.SaveChangesAsync();
                    break;
            }
        }

        private async Task HandleProfileAddressInput(OrderSession session, string message)
        {
            var success = await _profileManager.ProcessAddressInputAsync(session.BusinessId, session.PhoneNumber, message);
        }

        private async Task HandleProfileAddressRemovalInput(OrderSession session, string message)
        {
            var success = await _profileManager.ProcessAddressRemovalInputAsync(session.BusinessId, session.PhoneNumber, message);
        }

        private async Task HandleProfileMenuInput(OrderSession session, string message)
        {
            switch (message)
            {
                case "PROFILE_ADDRESSES":
                case "🏠 Manage Addresses":
                    await _profileManager.HandleAddressManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_PHONE":
                case "📞 Manage Contact Phone":
                    await _profileManager.HandlePhoneManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_CONTINUE_ORDER":
                case "🛒 Continue Order":
                    session.ProfileState = null;
                    await _db.SaveChangesAsync();
                    await ResumeOrderFromProfile(session);
                    break;
                case "PROFILE_BACK_TO_MENU":
                case "⬅️ Back":
                    await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    break;
                default:
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ Invalid option selected.." +
                        MessageFormattingHelper.FormatHelpContactFooter(session));
                    break;
            }
        }

        private async Task HandlePhoneMenuInput(OrderSession session, string message)
        {
            switch (message)
            {
                case "PROFILE_ADD_PHONE":
                case "📞 Add Contact Phone":
                    await _profileManager.HandleAddPhoneAsync(session.BusinessId, session.PhoneNumber);
                    break;

                case "PROFILE_REMOVE_PHONE":
                case "❌ Remove Contact Phone":
                    await _profileManager.HandleRemovePhoneConfirmationAsync(session.BusinessId, session.PhoneNumber);
                    break;

                case "PROFILE_BACK_TO_MENU":
                case "⬅️ Back":
                    await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    break;

                default:
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ Invalid option selected.");
                    await _profileManager.HandlePhoneManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;
            }
        }

        private async Task HandlePhoneConfirmationInput(OrderSession session, string message)
        {
            switch (message)
            {
                case "CONFIRM_REMOVE_PHONE_YES":
                case "✅ Yes, Remove":
                    await _profileManager.HandleRemovePhoneActionAsync(session.BusinessId, session.PhoneNumber);
                    break;

                case "CONFIRM_REMOVE_PHONE_NO":
                case "❎ No, Keep":
                    await _profileManager.HandlePhoneManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;

                default:
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "Please select Yes or No.");
                    break;
            }
        }
        private async Task HandleAddressMenuInput(OrderSession session, string message)
        {
            switch (message)
            {
                case "PROFILE_ADD_ADDRESS":
                case "➕ Add New Address":
                case "➕ Add Address":
                    await _profileManager.HandleAddAddressAsync(session.BusinessId, session.PhoneNumber);
                    break;
                    
                case "PROFILE_REMOVE_ADDRESS":
                case "🗑️ Remove Address":
                    await _profileManager.HandleRemoveAddressAsync(session.BusinessId, session.PhoneNumber);
                    break;
                    
                case "PROFILE_BACK_TO_MENU":
                case "⬅️ Back":
                    await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    break;
                    
                default:
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ Invalid option selected.." +
                        MessageFormattingHelper.FormatHelpContactFooter(session));
                    break;
            }
        }

        private async Task SendProfileManagementBlockedMessage(OrderSession session)
        {
            string message;

            switch (session.CurrentState)
            {
                case "COLLECT_NOTES":
                    message = "🔄 *Complete Notes First*\n\n" +
                            "You need to add special instructions for your order before managing your profile.\n\n" +
                            "Type your notes or send 'none' to skip, then you can manage your profile.";
                    break;
                    
                case "ADDRESS_SAVE_PROMPT":
                    message = "🔄 *Complete Address Decision*\n\n" +
                            "You need to decide whether to save your address before managing your profile.\n\n" +
                            "Choose 'Yes' or 'No' to continue.";
                    break;
                    
                case "ORDER_CONFIRMATION":
                    message = "🔄 *Complete Order First*\n\n" +
                            "You need to confirm your order before managing your profile.\n\n" +
                            "Confirm your order to proceed, then you can manage your profile.";
                    break;
                    
                case "ITEM_OPTIONS":
                    message = "🔄 *Complete Item Customization*\n\n" +
                            "You need to complete your item customization before managing your profile.\n\n" +
                            "Finish selecting your item options to continue.";
                    break;
                    
                default:
                    message = "🔄 *Flow Interrupted*\n\n" +
                            "Please complete the current step before managing your profile.\n\n" +
                            "Follow the instructions above to continue.";
                    break;
            }
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message + MessageFormattingHelper.FormatHelpContactFooter(session));
        }

        private async Task ResumeOrderFromProfile(OrderSession session)
        {
            var cart = _cartManager.DeserializeCart(session.CartData);
            
            switch (session.CurrentState)
            {
                case "LOCATION_SELECTION":
                    await _uiManager.ShowLocationSelection(session);
                    break;
                    
                case "ITEM_SELECTION":
                    await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Welcome back! Continue browsing our menu:");
                    break;
                    
                case "ITEM_OPTIONS":
                    var pendingParents = JsonConvert.DeserializeObject<List<PendingParent>>(session.PendingParents ?? "[]");
                    if (pendingParents.Any())
                    {
                        await _uiManager.ShowItemOptions(session.BusinessId, session.PhoneNumber, pendingParents.First());
                    }
                    else
                    {
                        await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Welcome back! Continue browsing our menu:");
                    }
                    break;
                    
                case "DELIVERY_METHOD":
                    await _uiManager.AskDeliveryMethod(session);
                    break;
                    
                case "DELIVERY_LOCATION_SELECTION":
                    await _uiManager.ShowDeliveryLocationSelection(session);
                    break;
                    
                case "DELIVERY_ADDRESS":
                    var hasAddresses = await _profileManager.HasAddressesAsync(session.BusinessId, session.PhoneNumber);
                    if (hasAddresses)
                    {
                        await _profileManager.ShowSavedAddressesForOrderAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else
                    {
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "📝 Please enter your delivery address:");
                    }
                    break;
                    
                case "COLLECT_NOTES":
                    await _stateManager.SendSpecialInstructionsPrompt(session);
                    break;
                    
                case "ORDER_CONFIRMATION":
                    await _uiManager.ShowOrderSummary(session, cart);
                    break;
                    
                default:
                    await _uiManager.SendWelcomeMessage(session.BusinessId, session.PhoneNumber, session.CustomerName);
                    break;
            }
        }

        private async Task HandleProfileAddAddressState(OrderSession session, string message)
        {
            if (message.Equals("manage profile", StringComparison.OrdinalIgnoreCase))
            {
                session.CurrentState = "LOCATION_SELECTION";
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                return;
            }

            var success = await _profileManager.ProcessAddressInputAsync(session.BusinessId, session.PhoneNumber, message);
            if (success)
            {
                await _sessionManager.DeleteTemporaryProfileSession(session.BusinessId, session.PhoneNumber);
                session.CurrentState = "LOCATION_SELECTION";
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
            }
            else
            {
            }
        }

        private async Task HandleProfileRemoveAddressState(OrderSession session, string message)
        {
            if (message.Equals("manage profile", StringComparison.OrdinalIgnoreCase))
            {
                session.CurrentState = "LOCATION_SELECTION";
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                return;
            }

            var success = await _profileManager.ProcessAddressRemovalInputAsync(session.BusinessId, session.PhoneNumber, message);
            if (success)
            {
                await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
            }
            else
            {
            }
        }

        private async Task HandleAddressSavePrompt(OrderSession session, OrderCart cart, string message)
        {
            var normalizedMessage = message.ToLower().Trim();
            
            if (message.Equals("PROFILE_BACK_TO_MENU", StringComparison.OrdinalIgnoreCase) || 
                message.Equals("⬅️ Back", StringComparison.OrdinalIgnoreCase))
            {
                session.CurrentState = "DELIVERY_ADDRESS";
                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "📝 Please enter your delivery address:");
                return;
            }

            var profileButtonPayloads = new[] { 
                "PROFILE_ADDRESSES",
                "PROFILE_ADD_ADDRESS", "PROFILE_REMOVE_ADDRESS", 
                "PROFILE_CONTINUE_ORDER", "PROFILE_BACK_TO_MAIN"
            };
            
            if (profileButtonPayloads.Contains(message))
            {
                return;
            }
            
            switch (message)
            {
                case "SAVE_ADDRESS_YES":
                case "✅ Yes, save it":
                case "yes":
                    var success = await _profileManager.SaveAddressDuringOrderAsync(session.BusinessId, session.PhoneNumber, session.DeliveryAddress);
                    
                    var hasContactPhoneAfterAddress = await _profileManager.HasContactPhoneAsync(session.BusinessId, session.PhoneNumber);
                    if (!hasContactPhoneAfterAddress)
                    {
                        session.CurrentState = "DELIVERY_CONTACT_PHONE";
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            MessageConstants.DeliveryContactPhonePrompt);
                        return;
                    }
                    
                    if (cart.Items.Any())
                    {
                        session.CurrentState = "COLLECT_NOTES";
                        await _stateManager.SendSpecialInstructionsPrompt(session);
                    }
                    else
                    {
                        session.CurrentState = "ITEM_SELECTION";
                        await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Click 'View items' to add items to your cart:");
                    }
                    break;
                    
                case "SAVE_ADDRESS_NO":
                case "👍 No, thanks":
                case "no":
                    
                    var hasContactPhoneAfterAddress_No = await _profileManager.HasContactPhoneAsync(session.BusinessId, session.PhoneNumber);
                    if (!hasContactPhoneAfterAddress_No)
                    {
                        session.CurrentState = "DELIVERY_CONTACT_PHONE";
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            MessageConstants.DeliveryContactPhonePrompt);
                        return;
                    }
                    
                    if (cart.Items.Any())
                    {
                        session.CurrentState = "COLLECT_NOTES";
                        await _stateManager.SendSpecialInstructionsPrompt(session);
                    }
                    else
                    {
                        session.CurrentState = "ITEM_SELECTION";
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "✅ Address not saved.\n\n" +
                            "Now add some items to your cart.");
                        await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Click 'View items' to add items to your cart:");
                    }
                    break;
                    
                default:
                    if (normalizedMessage.Contains("yes") && (normalizedMessage.Contains("save") || normalizedMessage.Contains("✅")))
                    {
                        var saveSuccess = await _profileManager.SaveAddressDuringOrderAsync(session.BusinessId, session.PhoneNumber, session.DeliveryAddress);
                        if (saveSuccess)
                        {
                        }
                        else
                        {
                        }
                        
                        var hasContactPhoneAfterAddress_FallbackYes = await _profileManager.HasContactPhoneAsync(session.BusinessId, session.PhoneNumber);
                        if (!hasContactPhoneAfterAddress_FallbackYes)
                        {
                            session.CurrentState = "DELIVERY_CONTACT_PHONE";
                            await _messagingService.SendTextMessageAsync(
                                session.BusinessId,
                                session.PhoneNumber,
                                MessageConstants.DeliveryContactPhonePrompt);
                            return;
                        }
                        
                        if (cart.Items.Any())
                        {
                            session.CurrentState = "COLLECT_NOTES";
                            await _stateManager.SendSpecialInstructionsPrompt(session);
                        }
                        else
                        {
                            session.CurrentState = "ITEM_SELECTION";
                            await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Click 'View items' to add items to your cart:");
                        }
                    }
                    else if (normalizedMessage.Contains("no") || normalizedMessage.Contains("thanks") || normalizedMessage.Contains("👍"))
                    {
                        var hasContactPhoneAfterAddress_FallbackNo = await _profileManager.HasContactPhoneAsync(session.BusinessId, session.PhoneNumber);
                        if (!hasContactPhoneAfterAddress_FallbackNo)
                        {
                            session.CurrentState = "DELIVERY_CONTACT_PHONE";
                            await _messagingService.SendTextMessageAsync(
                                session.BusinessId,
                                session.PhoneNumber,
                                MessageConstants.DeliveryContactPhonePrompt);
                            return;
                        }
                        
                        if (cart.Items.Any())
                        {
                            session.CurrentState = "COLLECT_NOTES";
                            await _stateManager.SendSpecialInstructionsPrompt(session);
                        }
                        else
                        {
                            session.CurrentState = "ITEM_SELECTION";
                            await _messagingService.SendTextMessageAsync(
                                session.BusinessId,
                                session.PhoneNumber,
                                "✅ Address not saved.\n\n" +
                                "Now add some items to your cart.");
                            await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Click 'View items' to add items to your cart:");
                        }
                    }
                    else
                    {
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "❌ Invalid option selected.\n\n" +
                            "Please use the buttons below." +
                            MessageFormattingHelper.FormatHelpContactFooter(session));
                    }
                    break;
            }
        }

        private async Task HandleProfileManagementWithSession(OrderSession session, string message)
        {
            if (!IsProfileManagementAllowed(session.CurrentState, message))
            {
                await SendProfileManagementBlockedMessage(session);
                return;
            }

            var normalizedMessage = message.ToLower().Trim();

            switch (message)
            {
                case "PROFILE_ADDRESSES":
                case "🏠 Manage Addresses":
                    await _profileManager.HandleAddressManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;

                case "PROFILE_ADD_ADDRESS":
                case "➕ Add New Address":
                case "➕ Add Address":
                    session.CurrentState = "PROFILE_ADD_ADDRESS";
                    session.ProfileState = "WAITING_FOR_ADDRESS";
                    await _db.SaveChangesAsync();
                    await _profileManager.HandleAddAddressAsync(session.BusinessId, session.PhoneNumber);
                    break;

                case "PROFILE_REMOVE_ADDRESS":
                case "🗑️ Remove Address":
                    await _profileManager.HandleRemoveAddressAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_PHONE":
                case "📞 Manage Contact Phone":
                    await _profileManager.HandlePhoneManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_ADD_PHONE":
                case "📞 Add Contact Phone":
                    session.ProfileState = "WAITING_FOR_PHONE";
                    await _db.SaveChangesAsync();
                    await _profileManager.HandleAddPhoneAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_REMOVE_PHONE":
                case "❌ Remove Contact Phone":
                    await _profileManager.HandleRemovePhoneConfirmationAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "CONFIRM_REMOVE_PHONE_YES":
                case "✅ Yes, Remove":
                    await _profileManager.HandleRemovePhoneActionAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "CONFIRM_REMOVE_PHONE_NO":
                case "❎ No, Keep":
                    await _profileManager.HandlePhoneManagementAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_BACK_TO_MENU":
                case "⬅️ Back":
                    await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    break;
                case "PROFILE_CONTINUE_ORDER":
                case "🛒 Continue Order":
                    session.ProfileState = null;
                    await _db.SaveChangesAsync();
                    await ResumeOrderFromProfile(session);
                    break;
                
                default:
                    if (normalizedMessage.Contains("address") && normalizedMessage.Contains("manage"))
                    {
                        await _profileManager.HandleAddressManagementAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else if (normalizedMessage.Contains("remove") && normalizedMessage.Contains("address"))
                    {
                        await _profileManager.HandleRemoveAddressAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else if (normalizedMessage.Contains("add") && normalizedMessage.Contains("address"))
                    {
                        await _profileManager.HandleAddAddressAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else if (normalizedMessage.Contains("yes") && (normalizedMessage.Contains("remove") || normalizedMessage.Contains("✅")))
                    {
                        var success2 = await _profileManager.ProcessAddressRemovalInputAsync(session.BusinessId, session.PhoneNumber, message);
                        if (success2)
                        {
                            await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                        }
                    }
                    else if (normalizedMessage.Contains("no") && (normalizedMessage.Contains("keep") || normalizedMessage.Contains("❌")))
                    {
                        await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else if (normalizedMessage.Contains("back"))
                    {
                        await _profileManager.HandleManageProfileAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else if (normalizedMessage.Contains("continue") && normalizedMessage.Contains("order"))
                    {

                        session.ProfileState = null;
                        await _db.SaveChangesAsync();

                        await ResumeOrderFromProfile(session);
                    }
                    else
                    {
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "❌ Invalid option selected.\n\n" +
                            "Please use the buttons below." +
                            MessageFormattingHelper.FormatHelpContactFooter(session));
                    }
                    break;
            }
        }

        private async Task HandleProfileCurrentState(OrderSession session, string message)
        {
            switch (session.CurrentState)
            {
                case "PROFILE_ADD_ADDRESS":
                    await HandleProfileAddAddressState(session, message);
                    break;
                case "PROFILE_REMOVE_ADDRESS":
                    await HandleProfileRemoveAddressState(session, message);
                    break;
                default:
                    session.CurrentState = "LOCATION_SELECTION";
                    await _db.SaveChangesAsync();
                    break;
            }
        }

        private string ExtractActionFromButton(string buttonMessage)
        {
            return buttonMessage switch
            {
                "START_ORDER" => "START_ORDER",
                "GET_HELP" => "HELP",
                "CONFIRM_ORDER" => "ORDER_CONFIRMATION",
                "EDIT_ORDER" => "EDIT_ORDER",
                "CANCEL_ORDER" => "CANCEL_ORDER",
                "APPLY_DISCOUNT" => "APPLY_DISCOUNT",
                "ADD_ITEM" => "ADD_ITEM",
                "REMOVE_ITEM" => "REMOVE_ITEM",
                "BACK_TO_SUMMARY" => "BACK_TO_SUMMARY",
                "SEARCH" => "SEARCH",
                "FULL_MENU" => "FULL_MENU",
                "🔍 Search Menu" => "SEARCH",
                "📖 Browse Menu" => "FULL_MENU",
                "PROFILE_ADDRESSES" => "PROFILE_MANAGEMENT",
                "PROFILE_ADD_ADDRESS" => "PROFILE_MANAGEMENT",
                "PROFILE_REMOVE_ADDRESS" => "PROFILE_MANAGEMENT",
                "PROFILE_BACK_TO_MENU" => "PROFILE_MANAGEMENT",
                "PROFILE_CONTINUE_ORDER" => "PROFILE_MANAGEMENT",
                "PROFILE_BACK_TO_MAIN" => "PROFILE_MANAGEMENT",
                "PROFILE_PHONE" => "PROFILE_MANAGEMENT",
                "PROFILE_ADD_PHONE" => "PROFILE_MANAGEMENT",
                "PROFILE_REMOVE_PHONE" => "PROFILE_MANAGEMENT",
                "CONFIRM_REMOVE_PHONE_YES" => "PROFILE_MANAGEMENT",
                "CONFIRM_REMOVE_PHONE_NO" => "PROFILE_MANAGEMENT",
                "HELP_BACK" => "START_ORDER",
                "VIEW_MORE_CATEGORIES" => "VIEW_MORE_CATEGORIES",
                "PROCEED_CHECKOUT" => "PROCEED_CHECKOUT",
                "ADD_MORE" => "ADD_MORE",
                "BROWSE_OTHERS" => "BROWSE_OTHERS",
                _ => "UNKNOWN"
            };
        }

        private async Task HandleActionNotAllowedInState(OrderSession session, string action)
        {
            var nextStep = _validationService.GetNextRequiredStep(session);
            
            var message = $"🔄 *Action Not Available*\n\n" +
                        $"The '{action}' action is not available at this stage.\n\n" +
                        "Complete the current step to continue." +
                        MessageFormattingHelper.FormatHelpContactFooter(session);
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message);
            
            await GuideUserToNextStep(session, nextStep);
        }

        private async Task HandleHelpNotAllowedInState(OrderSession session)
        {
            var nextStep = _validationService.GetNextRequiredStep(session);
            
            var message = $"🔄 *Help Not Available*\n\n" +
                        $"Help is not available at this stage.\n\n" +
                        "Complete the current step to continue." +
                        MessageFormattingHelper.FormatHelpContactFooter(session);
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message);
            
            await GuideUserToNextStep(session, nextStep);
        }

        private async Task HandleCancelNotAllowedInState(OrderSession session)
        {
            var nextStep = _validationService.GetNextRequiredStep(session);
            
            var message = $"🔄 *Cannot Cancel Now*\n\n" +
                        $"You cannot cancel at this stage.\n\n" +
                        "Complete the current step to continue." +
                        MessageFormattingHelper.FormatHelpContactFooter(session);
            
            await _messagingService.SendTextMessageAsync(
                session.BusinessId,
                session.PhoneNumber,
                message);
            
            await GuideUserToNextStep(session, nextStep);
        }

        private async Task GuideUserToNextStep(OrderSession session, string nextStep)
        {
            switch (nextStep)
            {
                case "LOCATION_SELECTION":
                    await _uiManager.ShowLocationSelection(session);
                    break;
                    
                case "DELIVERY_METHOD":
                    await _uiManager.AskDeliveryMethod(session);
                    break;
                    
                case "DELIVERY_ADDRESS":
                    var hasAddresses = await _profileManager.HasAddressesAsync(session.BusinessId, session.PhoneNumber);
                    if (hasAddresses)
                    {
                        await _profileManager.ShowSavedAddressesForOrderAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else
                    {
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "📝 Please enter your delivery address:");
                    }
                    break;
                    
                case "COLLECT_NOTES":
                    await _stateManager.SendSpecialInstructionsPrompt(session);
                    break;
                    
                case "ORDER_CONFIRMATION":
                    var cart = _cartManager.DeserializeCart(session.CartData);
                    await _uiManager.ShowOrderSummary(session, cart);
                    break;
                    
                default:
                    await _uiManager.SendWelcomeMessage(session.BusinessId, session.PhoneNumber, session.CustomerName);
                    break;
            }
        }

        private async Task HandleMismatchedButtonClick(OrderSession session, string message)
        {
            var cart = _cartManager.DeserializeCart(session.CartData);
            
            switch (session.CurrentState)
            {
                case "ORDER_CONFIRMATION":
                    await _uiManager.ShowOrderSummary(session, cart);
                    break;
                case "ADDRESS_SAVE_PROMPT":
                    await HandleAddressSavePrompt(session, cart, message);
                    break;
                case "COLLECT_NOTES":
                    await _stateManager.SendSpecialInstructionsPrompt(session);
                    break;
                case "ITEM_OPTIONS":
                    var pendingParents = JsonConvert.DeserializeObject<List<PendingParent>>(session.PendingParents ?? "[]");
                    if (pendingParents.Any())
                    {
                        await _uiManager.ShowItemOptions(session.BusinessId, session.PhoneNumber, pendingParents.First());
                    }
                    break;
                case "DELIVERY_METHOD":
                    await _uiManager.AskDeliveryMethod(session);
                    break;
                case "DELIVERY_LOCATION_SELECTION":
                    await _uiManager.ShowDeliveryLocationSelection(session);
                    break;
                case "DELIVERY_ADDRESS":
                    var hasAddresses = await _profileManager.HasAddressesAsync(session.BusinessId, session.PhoneNumber);
                    if (hasAddresses)
                    {
                        await _profileManager.ShowSavedAddressesForOrderAsync(session.BusinessId, session.PhoneNumber);
                    }
                    else
                    {
                        await _messagingService.SendTextMessageAsync(
                            session.BusinessId,
                            session.PhoneNumber,
                            "📝 Please enter your delivery address:");
                    }
                    break;
                case "LOCATION_SELECTION":
                    await _uiManager.ShowLocationSelection(session);
                    break;
                case "CANCEL_CONFIRMATION":
                    await _stateManager.HandleCancelConfirmationResponse(session, cart, message);
                    break;
                case "REMOVE_ITEM_PROMPT":
                    await _stateManager.HandleRemoveItemByNumber(session, cart, message);
                    break;
                case "EDIT_ORDER":
                    await _uiManager.ShowEditOrderMenu(session, cart);
                    break;
                case "PACK_SELECTION_ADD":
                case "PACK_SELECTION_REMOVE":
                    var action = session.CurrentState == "PACK_SELECTION_ADD" ? "ADD" : "REMOVE";
                    await _uiManager.ShowPackSelectionMenu(session, cart, action);
                    break;
                default:
                    await SendGenericPrompt(session);
                    break;
            }
        }

        private bool IsMismatchedButtonClick(OrderSession session, string message)
        {
            var editOrderButtons = new[] { "ADD_ITEM", "REMOVE_ITEM", "BACK_TO_SUMMARY" };
            var catalogNavigationButtons = new[] { 
                "CAT_", "SUBCAT_", "CAT_SET_", "VIEW_MORE_CATEGORIES", 
                "BACK_CATEGORIES", "BACK_SUBCATEGORIES", "BACK_TO_MAIN"
            };
            var profileManagementButtons = new[] { 
                "PROFILE_", "MANAGE_PROFILE", "📧", "🏠", "✏️", "➕", "🗑️"
            };
            var orderFlowButtons = new[] { 
                "START_ORDER", "PROCEED_CHECKOUT", "ADD_MORE", "BROWSE_OTHERS"
            };

            if (session.CurrentState == "ORDER_CONFIRMATION" && 
                editOrderButtons.Contains(message))
            {
                return true;
            }

            var collectionStates = new[] { 
                "ADDRESS_SAVE_PROMPT", "COLLECT_NOTES" 
            };
            
            if (collectionStates.Contains(session.CurrentState))
            {
                var invalidButtons = new[] { "ADD_ITEM", "EDIT_ORDER", "START_ORDER" }
                    .Concat(catalogNavigationButtons.Where(btn => message.StartsWith(btn)))
                    .Concat(orderFlowButtons);
                
                if (invalidButtons.Any(btn => message == btn || message.StartsWith(btn)))
                {
                    return true;
                }
            }

            if (session.CurrentState.StartsWith("PROFILE_") || !string.IsNullOrEmpty(session.ProfileState))
            {
                if (orderFlowButtons.Any(btn => message == btn) || 
                    catalogNavigationButtons.Any(btn => message.StartsWith(btn)))
                {
                    return true;
                }
            }

            if ((session.CurrentState == "ITEM_SELECTION" || session.CurrentState == "ITEM_OPTIONS") &&
                (message == "CONFIRM_ORDER" || message == "CANCEL_ORDER"))
            {
                return true;
            }

            var deliveryFlowStates = new[] { 
                "DELIVERY_METHOD", "DELIVERY_LOCATION_SELECTION", "DELIVERY_ADDRESS" 
            };
            
            if (deliveryFlowStates.Contains(session.CurrentState) &&
                catalogNavigationButtons.Any(btn => message.StartsWith(btn)))
            {
                return true;
            }

            return false;
        }

        private async Task HandleAddressSelection(OrderSession session, string message)
        {
            if (message.StartsWith("ADDR_PAGE_"))
            {
                var pageStr = message["ADDR_PAGE_".Length..];
                if (!int.TryParse(pageStr, out var page) || page < 1) page = 1;
                await _profileManager.ShowSavedAddressesForOrderAsync(session.BusinessId, session.PhoneNumber, page);
                return;
            }

            if (message == "ADD_NEW_ADDRESS")
            {
                session.CurrentState = "DELIVERY_ADDRESS";
                await _db.SaveChangesAsync();

                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "🏠 Add New Address\n\n" +
                    "Please enter your delivery address (e.g., 123 Main Street, Lagos, Nigeria):");
                return;
            }

            int addressIndex = -1;

            if (message.StartsWith("ADDRESS_"))
            {
                var addressIndexStr = message["ADDRESS_".Length..];
                if (!int.TryParse(addressIndexStr, out addressIndex) || addressIndex < 1)
                {
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ Invalid address selection.\n\nPlease select a valid address from the list." +
                        MessageFormattingHelper.FormatHelpContactFooter(session));
                    return;
                }
            }
            else if (message.StartsWith("Address "))
            {
                var addressIndexStr = message.Substring("Address ".Length);
                if (!int.TryParse(addressIndexStr, out addressIndex) || addressIndex < 1)
                {
                    await _messagingService.SendTextMessageAsync(
                        session.BusinessId,
                        session.PhoneNumber,
                        "❌ Invalid address selection.\n\nPlease select a valid address from the list." +
                        MessageFormattingHelper.FormatHelpContactFooter(session));
                    return;
                }
            }
            else
            {
                return;
            }

            var selectedAddress = await _profileManager.GetSavedAddressForOrderAsync(session.BusinessId, session.PhoneNumber, addressIndex);
            if (string.IsNullOrEmpty(selectedAddress))
            {
                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "❌ Failed to retrieve the selected address.\n\nPlease try selecting again or enter a new address." +
                    MessageFormattingHelper.FormatHelpContactFooter(session));
                return;
            }

            session.DeliveryAddress = selectedAddress;
            await _db.SaveChangesAsync();

            var hasContactPhone = await _profileManager.HasContactPhoneAsync(session.BusinessId, session.PhoneNumber);
            if (!hasContactPhone)
            {
                session.CurrentState = "DELIVERY_CONTACT_PHONE";
                await _db.SaveChangesAsync();
                
                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "📱 *Contact Phone Required*\n\n" +
                    "Please enter a phone number to contact you for delivery:\n\n" +
                    "Example: 08012345678");
                return;
            }

            var cart = _cartManager.DeserializeCart(session.CartData);

            if (cart.Items.Any())
            {
                session.CurrentState = "COLLECT_NOTES";
                await _stateManager.SendSpecialInstructionsPrompt(session);
            }
            else
            {
                session.CurrentState = "ITEM_SELECTION";
                await _messagingService.SendTextMessageAsync(
                    session.BusinessId,
                    session.PhoneNumber,
                    "Now add some items to your cart.");
                await _uiManager.ShowMainMenu(session.BusinessId, session.PhoneNumber, "Click 'View items' to add items to your cart:");
            }

            await _db.SaveChangesAsync();
        }
    }
}
