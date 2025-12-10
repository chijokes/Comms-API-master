using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using FusionComms.DTOs.WhatsApp;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class OrderSessionManager
    {
        private readonly AppDbContext _db;
        private readonly WhatsAppMessagingService _messagingService;

        public OrderSessionManager(AppDbContext dbContext, WhatsAppMessagingService messagingService)
        {
            _db = dbContext;
            _messagingService = messagingService;
        }

        public async Task<OrderSession> GetOrCreateSession(string businessId, string rawPhoneNumber)
        {
            var phoneNumber = FormatWhatsAppPhoneNumber(rawPhoneNumber);
            var session = await _db.OrderSessions
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.PhoneNumber == phoneNumber);

            if (session == null)
            {
                session = new OrderSession
                {
                    BusinessId = businessId,
                    PhoneNumber = phoneNumber,
                    CurrentState = "LOCATION_SELECTION",
                    CartData = JsonConvert.SerializeObject(new OrderCart())
                };
                _db.OrderSessions.Add(session);
                await _db.SaveChangesAsync();
            }
            else
            {
                if (session.CurrentState == "CANCELLED")
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
                    session.DiscountCode = null;
                    session.DiscountAmount = 0;
                    session.DiscountType = null;
                    session.DiscountValue = 0;

                    session.LastInteraction = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    await _db.Entry(session).ReloadAsync();
                }
            }
            return session;
        }

        public async Task<OrderSession> GetExistingSession(string businessId, string phoneNumber)
        {
            var formattedPhoneNumber = FormatWhatsAppPhoneNumber(phoneNumber);
            return await _db.OrderSessions
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.PhoneNumber == formattedPhoneNumber);
        }

        public async Task UpdateSessionState(OrderSession session, string newState)
        {
            session.CurrentState = newState;
            session.LastInteraction = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task UpdateCustomerName(OrderSession session, string customerName)
        {
            if (!string.IsNullOrEmpty(customerName) && string.IsNullOrEmpty(session.CustomerName))
            {
                session.CustomerName = customerName;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateSession(OrderSession session)
        {
            session.LastInteraction = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public static string FormatWhatsAppPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.StartsWith("+")) return phoneNumber;

            if (phoneNumber.Contains("@"))
            {
                return "+" + phoneNumber.Split('@')[0];
            }

            return "+" + phoneNumber;
        }

        public async Task CleanupOldSessions()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24);
                var oldSessions = await _db.OrderSessions
                    .Where(s => s.LastInteraction < cutoffTime)
                    .ToListAsync();

                if (oldSessions.Any())
                {
                    _db.OrderSessions.RemoveRange(oldSessions);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CleanupCancelledSessions()
        {
            try
            {
                var cancelledSessions = await _db.OrderSessions
                    .Where(s => s.CurrentState == "CANCELLED")
                    .ToListAsync();

                if (cancelledSessions.Any())
                {
                    _db.OrderSessions.RemoveRange(cancelledSessions);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteSession(string businessId, string phoneNumber)
        {
            try
            {
                var formattedPhoneNumber = FormatWhatsAppPhoneNumber(phoneNumber);
                var session = await _db.OrderSessions
                    .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.PhoneNumber == formattedPhoneNumber);

                if (session != null)
                {
                    _db.OrderSessions.Remove(session);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteTemporaryProfileSession(string businessId, string phoneNumber)
        {
            try
            {
                var formattedPhoneNumber = FormatWhatsAppPhoneNumber(phoneNumber);
                var profileSessions = await _db.OrderSessions
                    .Where(s => s.BusinessId == businessId && 
                            s.PhoneNumber == formattedPhoneNumber && 
                            s.CurrentState.StartsWith("PROFILE_"))
                    .ToListAsync();

                if (profileSessions.Any())
                {
                    _db.OrderSessions.RemoveRange(profileSessions);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
