using FusionComms.DTOs.WhatsApp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class ProfileManager
    {
        private readonly WhatsAppProfileService _profileService;
        private readonly WhatsAppMessagingService _messagingService;
        private readonly OrderSessionManager _sessionManager;

        public ProfileManager(WhatsAppProfileService profileService, WhatsAppMessagingService messagingService, OrderSessionManager sessionManager)
        {
            _profileService = profileService;
            _messagingService = messagingService;
            _sessionManager = sessionManager;
        }

        public async Task<bool> HasAddressesAsync(string businessId, string phoneNumber)
        {
            var result = await _profileService.HasAddressesAsync(businessId, phoneNumber);
            return result;
        }

        public async Task<bool> HasContactPhoneAsync(string businessId, string phoneNumber)
        {
            return await _profileService.HasContactPhoneAsync(businessId, phoneNumber);
        }

        public async Task<string> GetSavedAddressForOrderAsync(string businessId, string phoneNumber, int index)
        {
            var addresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);
            if (index > 0 && index <= addresses.Count)
            {
                var address = addresses[index - 1].Address;
                return address;
            }
            return null;
        }

        public async Task ShowSavedAddressesForOrderAsync(string businessId, string phoneNumber, int page = 1)
        {
            var addresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);
            
            if (addresses.Count == 0)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "📝 No saved addresses found. Please enter your delivery address:");
                return;
            }

            const int pageSize = 8;
            var total = addresses.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));
            var offset = (page - 1) * pageSize;
            var paged = addresses.Skip(offset).Take(pageSize).ToList();

            var rows = paged.Select((addr, index) => new WhatsAppRow
            {
                Id = $"ADDRESS_{offset + index + 1}",
                Title = $"Address {offset + index + 1}",
                Description = TruncateAddress(addr.Address, 72)
            }).ToList();

            var hasPrev = page > 1;
            var hasNext = page < totalPages;
            if (hasPrev)
            {
                rows.Add(new WhatsAppRow { Id = $"ADDR_PAGE_{page - 1}", Title = "⬅️ Prev", Description = "Go to previous page" });
            }
            if (hasNext)
            {
                rows.Add(new WhatsAppRow { Id = $"ADDR_PAGE_{page + 1}", Title = "Next ➡️", Description = "See more addresses" });
            }

            var sections = new List<WhatsAppSection>
            {
                new WhatsAppSection
                {
                    Title = "Saved Addresses",
                    Rows = rows
                },
                new WhatsAppSection
                {
                    Title = "Other Options",
                    Rows = new List<WhatsAppRow>
                    {
                        new WhatsAppRow
                        {
                            Id = "ADD_NEW_ADDRESS",
                            Title = "➕ Add New Address",
                            Description = "Enter a new delivery address"
                        }
                    }
                }
            };

            var bodyText = $"🏠 *Select Delivery Address*\n\nPage {page} of {totalPages} • Choose from your {addresses.Count} saved address(es):";
            
            await _messagingService.SendInteractiveListAsync(
                businessId,
                phoneNumber,
                bodyText,
                "Select Address",
                sections);
        }

        private string TruncateAddress(string address, int maxLength)
        {
            if (string.IsNullOrEmpty(address) || address.Length <= maxLength)
                return address;

            var truncated = address.Substring(0, maxLength - 3);
            var lastSpaceIndex = truncated.LastIndexOf(' ');
            
            if (lastSpaceIndex > maxLength * 0.7)
            {
                truncated = truncated.Substring(0, lastSpaceIndex);
            }
            
            return truncated + "...";
        }

        public async Task HandleManageProfileAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "PROFILE_MENU";
                await _sessionManager.UpdateSession(session);
            }

            var sections = new List<WhatsAppSection>
            {
                new WhatsAppSection
                {
                    Title = "Profile",
                    Rows = new List<WhatsAppRow>
                    {
                        new WhatsAppRow
                        {
                            Id = "PROFILE_ADDRESSES",
                            Title = "🏠 Manage Addresses",
                            Description = "Add or remove saved addresses"
                        },
                        new WhatsAppRow
                        {
                            Id = "PROFILE_PHONE",
                            Title = "📞 Manage Contact Phone",
                            Description = "Add or remove contact phone"
                        }
                    }
                },
                new WhatsAppSection
                {
                    Title = "Other",
                    Rows = new List<WhatsAppRow>
                    {
                        new WhatsAppRow
                        {
                            Id = "PROFILE_CONTINUE_ORDER",
                            Title = "🛒 Continue Order",
                            Description = "Return to ordering"
                        }
                    }
                }
            };

            var bodyText = "⚙️ *Profile Management*\n\nSelect an Option:";

            await _messagingService.SendInteractiveListAsync(
                businessId,
                phoneNumber,
                bodyText,
                "Choose",
                sections);
        }

        public async Task HandleAddressManagementAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "ADDRESS_MENU";
                await _sessionManager.UpdateSession(session);
            }
            
            var addresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);

            var buttons = new List<WhatsAppButton>();
            string message;
            
            if (addresses.Count > 0)
            {
                message = $"🏠 *Address Management*\n\nYou have {addresses.Count} saved address(es):\n\n";
                for (int i = 0; i < addresses.Count; i++)
                {
                    message += $"{i + 1}. {addresses[i].Address}\n";
                }
                message += "\nWhat would you like to do?";
                
                if (addresses.Count < 9)
                {
                    buttons.Add(new() { Text = "➕ Add New Address", Payload = "PROFILE_ADD_ADDRESS" });
                }
                else
                {
                    message += "\n⚠️ *Maximum of 9 addresses reached*";
                }
                
                buttons.Add(new() { Text = "🗑️ Remove Address", Payload = "PROFILE_REMOVE_ADDRESS" });
                buttons.Add(new() { Text = "⬅️ Back", Payload = "PROFILE_BACK_TO_MENU" });
            }
            else
            {
                message = "🏠 *Address Management*\n\nNo addresses saved.\n\nWhat would you like to do?";
                buttons.Add(new() { Text = "➕ Add New Address", Payload = "PROFILE_ADD_ADDRESS" });
                buttons.Add(new() { Text = "⬅️ Back", Payload = "PROFILE_BACK_TO_MENU" });
            }

            await _messagingService.SendInteractiveMessageAsync(businessId, phoneNumber, message, buttons);
        }

        public async Task HandlePhoneManagementAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "PHONE_MENU";
                await _sessionManager.UpdateSession(session);
            }

            var hasPhone = await _profileService.HasContactPhoneAsync(businessId, phoneNumber);
            var currentPhone = await _profileService.GetContactPhoneAsync(businessId, phoneNumber);

            var buttons = new List<WhatsAppButton>();
            string message;

            if (hasPhone && !string.IsNullOrEmpty(currentPhone))
            {
                message = $"📞 *Contact Phone*\n\n" +
                            $"Current saved number: *{currentPhone}*\n\n" +
                            "What would you like to do?";
                
                buttons.Add(new() { Text = "❌ Remove Phone", Payload = "PROFILE_REMOVE_PHONE" });
            }
            else
            {
                message = "📞 *Contact Phone*\n\n" +
                            "No contact phone saved.\n\n" +
                            "What would you like to do?";
                
                buttons.Add(new() { Text = "📞 Add Contact Phone", Payload = "PROFILE_ADD_PHONE" });
            }

            buttons.Add(new() { Text = "⬅️ Back", Payload = "PROFILE_BACK_TO_MENU" });

            await _messagingService.SendInteractiveMessageAsync(businessId, phoneNumber, message, buttons);
        }

        public async Task HandleRemovePhoneConfirmationAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "CONFIRM_PHONE_REMOVAL";
                await _sessionManager.UpdateSession(session);
            }

            var buttons = new List<WhatsAppButton>
            {
                new() { Text = "✅ Yes, Remove", Payload = "CONFIRM_REMOVE_PHONE_YES" },
                new() { Text = "❎ No, Keep", Payload = "CONFIRM_REMOVE_PHONE_NO" }
            };

            await _messagingService.SendInteractiveMessageAsync(
                businessId,
                phoneNumber,
                "⚠️ *Confirm Removal*\n\nAre you sure you want to remove your saved contact phone?",
                buttons);
        }

        public async Task HandleRemovePhoneActionAsync(string businessId, string phoneNumber)
        {
            var success = await _profileService.RemoveContactPhoneAsync(businessId, phoneNumber);

            if (success)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "✅ Contact phone removed successfully.");
            }
            else
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Failed to remove phone. Please try again.");
            }

            await HandlePhoneManagementAsync(businessId, phoneNumber);
        }

        public async Task HandleAddAddressAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "WAITING_FOR_ADDRESS";
                await _sessionManager.UpdateSession(session);
            }
            
            await _messagingService.SendTextMessageAsync(
                businessId,
                phoneNumber,
                "🏠 *Add New Address*\n\n" +
                "Please enter your delivery address:\n\n" +
                "Example: 123 Main Street, Lagos, Nigeria");
        }

        public async Task HandleRemoveAddressAsync(string businessId, string phoneNumber)
        {
            var addresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);
            if (addresses.Count == 0)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ No addresses to remove.");
                return;
            }

            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "WAITING_FOR_ADDRESS_REMOVAL";
                await _sessionManager.UpdateSession(session);
            }

            var message = "🗑️ *Remove Address*\n\n";
            message += "Which address would you like to remove?\n\n";
            for (int i = 0; i < addresses.Count; i++)
            {
                message += $"{i + 1}. {addresses[i].Address}\n";
            }
            message += "\nReply with the number to remove that address.";

            await _messagingService.SendTextMessageAsync(businessId, phoneNumber, message);
        }

        public async Task HandleAddPhoneAsync(string businessId, string phoneNumber)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session != null)
            {
                session.ProfileState = "WAITING_FOR_PHONE";
                await _sessionManager.UpdateSession(session);
            }

            await _messagingService.SendTextMessageAsync(
                businessId,
                phoneNumber,
                "📱 Please enter your contact phone number:\n\n" +
                "Example: +2348012345678 or 08012345678");
        }

        public async Task<bool> ProcessPhoneInputAsync(string businessId, string phoneNumber, string input)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session == null || session.ProfileState != "WAITING_FOR_PHONE")
            {
                return false;
            }

            var normalized = NormalizeContactPhone(input);
            if (string.IsNullOrEmpty(normalized))
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Invalid phone format. Please enter a valid phone number.");
                return false;
            }

            var success = await _profileService.SaveContactPhoneAsync(businessId, phoneNumber, normalized);
            if (success)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "✅ Contact phone saved successfully!");

                session.ProfileState = "PROFILE_MENU";
                await _sessionManager.UpdateSession(session);
                await HandleManageProfileAsync(businessId, phoneNumber);
                return true;
            }
            else
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Failed to save phone. Please try again.");
                return false;
            }
        }

        private string NormalizeContactPhone(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = new string(input.Where(char.IsDigit).ToArray());
            if (s.Length < 7 || s.Length > 15) return null;
            if (input.Trim().StartsWith("+"))
            {
                return "+" + s;
            }
            if (input.Trim().StartsWith("0"))
            {
                return s.StartsWith("0") ? s : "0" + s;
            }
            return s;
        }

        public async Task<bool> ProcessAddressInputAsync(string businessId, string phoneNumber, string address)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session == null || session.ProfileState != "WAITING_FOR_ADDRESS")
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "⚠️ *Address Input Not Allowed*\n\n" +
                    "Addresses can only be added through the profile management menu.\n\n" +
                    "Type 'manage profile' to access the menu and click the 'Add Address' button.");
                return false;
            }
            
            var existingAddresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);
            if (existingAddresses.Count >= 9)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ *Maximum Addresses Reached*\n\n" +
                    "You already have the maximum of 9 saved addresses.\n\n" +
                    "Please remove an existing address before adding a new one.");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(address) || address.Length < 10)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Address too short. Please enter at least 10 characters.");
                return false;
            }

            var addressLower = address.ToLower();
            
            var blockedPatterns = new[] { 
                "manage profile", "help", "menu", "cancel", "back", "continue",
                "✅", "❌", "🗑️", "📧", "🏠", "⬅️", "➕", "✏️"
            };
            
            var hasBlockedPatterns = blockedPatterns.Any(pattern => addressLower.Contains(pattern));
            
            if (hasBlockedPatterns)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ That doesn't look like a valid address. Please enter a real delivery address without emojis or system commands.\n\n" +
                    "Type 'manage profile' to return to profile menu");
                return false;
            }

            var success = await _profileService.AddAddressAsync(businessId, phoneNumber, address.Trim());
            if (success)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "✅ Address saved successfully!");
                
                if (session != null)
                {
                    if (session.CurrentState.StartsWith("PROFILE_"))
                    {
                        session.ProfileState = "PROFILE_MENU";
                        await _sessionManager.UpdateSession(session);
                        
                        await HandleManageProfileAsync(businessId, phoneNumber);
                    }
                    else
                    {
                        session.ProfileState = "PROFILE_MENU";
                        await _sessionManager.UpdateSession(session);
                        
                        await HandleManageProfileAsync(businessId, phoneNumber);
                    }
                }
                
                return true;
            }
            else
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Failed to save address. Please try again.");
                return false;
            }
        }

        public async Task<bool> SaveAddressDuringOrderAsync(string businessId, string phoneNumber, string address)
        {
            var existingAddresses = await _profileService.GetAddressesAsync(businessId, phoneNumber);
            if (existingAddresses.Count >= 9)
            {
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            var addressLower = address.ToLower();
            var blockedPatterns = new[] { 
                "manage profile", "help", "menu", "cancel", "back", "continue",
                "✅", "❌", "🗑️", "📧", "🏠", "⬅️", "➕", "✏️"
            };
            var hasBlockedPatterns = blockedPatterns.Any(pattern => addressLower.Contains(pattern));
            
            if (hasBlockedPatterns)
            {
                return false;
            }

            var success = await _profileService.AddAddressAsync(businessId, phoneNumber, address.Trim());
            if (success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> ProcessAddressRemovalInputAsync(string businessId, string phoneNumber, string input)
        {
            var session = await _sessionManager.GetExistingSession(businessId, phoneNumber);
            if (session == null || session.ProfileState != "WAITING_FOR_ADDRESS_REMOVAL")
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "⚠️ *Address Removal Not Allowed*\n\n" +
                    "Addresses can only be removed through the profile management menu.\n\n" +
                    "Type 'manage profile' to access the menu and click the 'Remove Address' button.");
                return false;
            }
            
            if (!int.TryParse(input, out int index) || index < 1)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Invalid selection. Please enter a valid number.");
                return false;
            }

            var success = await _profileService.RemoveAddressByIndexAsync(businessId, phoneNumber, index);
            if (success)
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "✅ Address removed successfully!");
                
                if (session != null)
                {
                    session.ProfileState = "PROFILE_MENU";
                    await _sessionManager.UpdateSession(session);
                }
                
                await HandleManageProfileAsync(businessId, phoneNumber);
                return true;
            }
            else
            {
                await _messagingService.SendTextMessageAsync(
                    businessId,
                    phoneNumber,
                    "❌ Failed to remove address. Please try again.");
                return false;
            }
        }
    }
}
