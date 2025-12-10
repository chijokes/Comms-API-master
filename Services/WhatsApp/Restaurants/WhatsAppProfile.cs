using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;


namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class WhatsAppProfileService
    {
        private readonly AppDbContext _db;

        public WhatsAppProfileService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<CustomerProfile> GetOrCreateProfileAsync(string businessId, string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            
            var profile = await _db.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.BusinessId == businessId && p.PhoneNumber == normalizedPhone);

            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    BusinessId = businessId,
                    PhoneNumber = normalizedPhone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.CustomerProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            return profile;
        }

        public async Task<CustomerProfile> GetProfileAsync(string businessId, string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            
            var profile = await _db.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.BusinessId == businessId && p.PhoneNumber == normalizedPhone);

            return profile;
        }

        public async Task<bool> AddAddressAsync(string businessId, string phoneNumber, string address)
        {
            try
            {
                var profile = await GetOrCreateProfileAsync(businessId, phoneNumber);
                
                var newAddress = new CustomerAddress
                {
                    ProfileId = profile.ProfileId,
                    Address = address,
                    CreatedAt = DateTime.UtcNow
                };

                profile.Addresses.Add(newAddress);
                profile.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemoveAddressByIndexAsync(string businessId, string phoneNumber, int index)
        {
            try
            {
                var profile = await GetProfileAsync(businessId, phoneNumber);
                if (profile?.Addresses != null && index > 0 && index <= profile.Addresses.Count)
                {
                    var addressToRemove = profile.Addresses.ElementAt(index - 1);
                    _db.Set<CustomerAddress>().Remove(addressToRemove);
                    
                    profile.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<CustomerAddress>> GetAddressesAsync(string businessId, string phoneNumber)
        {
            var profile = await GetProfileAsync(businessId, phoneNumber);
            var addresses = profile?.Addresses?.ToList() ?? new List<CustomerAddress>();

            return addresses;
        }

        public async Task<bool> HasAddressesAsync(string businessId, string phoneNumber)
        {
            var profile = await GetProfileAsync(businessId, phoneNumber);
            var hasAddresses = profile?.Addresses?.Any() == true;

            return hasAddresses;
        }

        public async Task<bool> SaveContactPhoneAsync(string businessId, string phoneNumber, string contactPhone)
        {
            try
            {
                var profile = await GetOrCreateProfileAsync(businessId, phoneNumber);
                profile.ContactPhone = contactPhone;
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save contact phone for businessId {BusinessId} and phoneNumber {PhoneNumber}", businessId, phoneNumber);
                return false;
            }
        }

        public async Task<bool> HasContactPhoneAsync(string businessId, string phoneNumber)
        {
            var profile = await GetProfileAsync(businessId, phoneNumber);

            if (profile == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(profile.ContactPhone);
        }

        public async Task<string> GetContactPhoneAsync(string businessId, string phoneNumber)
        {
            var profile = await GetProfileAsync(businessId, phoneNumber);
            return profile?.ContactPhone;
        }

        public async Task<bool> RemoveContactPhoneAsync(string businessId, string phoneNumber)
        {
            try
            {
                var profile = await GetProfileAsync(businessId, phoneNumber);
                if (profile == null) return false;
                profile.ContactPhone = null;
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            if (phoneNumber.StartsWith("+"))
                return phoneNumber;

            if (phoneNumber.Contains("@"))
            {
                return "+" + phoneNumber.Split('@')[0];
            }

            return "+" + phoneNumber;
        }
    }
}
