using FusionComms.DTOs.WhatsApp;
using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class OrderService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderService(IHttpClientFactory httpClientFactory, AppDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _db = dbContext;
        }

        public async Task<List<WhatsAppProductSet>> GetProductSets(string businessId)
        {
            return await _db.WhatsAppProductSets
                .Where(ps => ps.BusinessId == businessId)
                .ToListAsync();
        }

        public async Task<List<WhatsAppProduct>> GetProductsBySetIds(string businessId, List<string> setIds, string revenueCenterId)
        {
            if (setIds == null || setIds.Count == 0) return new List<WhatsAppProduct>();

            return await _db.WhatsAppProducts
                .Where(p => setIds.Contains(p.SetId))
                .Where(p => !string.IsNullOrEmpty(p.RetailerId))
                .Where(p => string.IsNullOrEmpty(revenueCenterId) || p.RevenueCenterId == revenueCenterId)
                .ToListAsync();
        }

        public async Task<List<WhatsAppProductSetGrouping>> GetProductSetGroupings(string businessId)
        {
            return await _db.WhatsAppProductSetGroupings
                .Where(g => g.BusinessId == businessId)
                .ToListAsync();
        }

        public async Task<WhatsAppProductSetGrouping> GetProductSetGroupingById(string businessId, string groupingId)
        {
            return await _db.WhatsAppProductSetGroupings
                .FirstOrDefaultAsync(g => g.BusinessId == businessId && g.Id == groupingId);
        }

        public List<string> ParseGroupingSetIds(WhatsAppProductSetGrouping grouping)
        {
            if (grouping == null || string.IsNullOrWhiteSpace(grouping.ProductSetIds)) return new List<string>();
            try
            {
                var arr = JsonConvert.DeserializeObject<List<string>>(grouping.ProductSetIds);
                return arr?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<string>> GetSubcategoriesForSets(string businessId, List<string> setIds, string revenueCenterId)
        {
            if (setIds == null || setIds.Count == 0) return new List<string>();

            var subs = await _db.WhatsAppProducts
                .Where(p => setIds.Contains(p.SetId))
                .Where(p => !string.IsNullOrEmpty(p.Subcategory))
                .Where(p => !string.IsNullOrEmpty(p.RetailerId))
                .Where(p => string.IsNullOrEmpty(revenueCenterId) || p.RevenueCenterId == revenueCenterId)
                .Select(p => p.Subcategory)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return subs;
        }

        public async Task<List<CatalogItem>> GetItemsAsync(string restaurantId, string revenueCenterId, List<string> itemIds = null)
        {
            using var client = _httpClientFactory.CreateClient();
            
            var url = $"https://api.food-ease.io/api/v1/Item/whatsapp-items?restaurantId={restaurantId}&revenueCenterId={revenueCenterId}";
            
            if (itemIds != null && itemIds.Any()) 
            {
                foreach (var itemId in itemIds)
                {
                    url += $"&itemIds={itemId}";
                }
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Catalog fetch failed: {response.StatusCode} - URL: {url}");

            var responseContent = await response.Content.ReadAsStringAsync();

            var wrappedResponse = JsonConvert.DeserializeObject<CatalogApiResponse>(responseContent);
            
            return wrappedResponse?.Data ?? new List<CatalogItem>();
        }

        public async Task<List<ToppingItem>> GetToppingsAsync(string toppingClassId, string revenueCenterId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var url = $"https://api.food-ease.io/api/v1/Item/list-items-by-item-class?itemClassId={toppingClassId}&revenueCenterId={revenueCenterId}";

                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ToppingListResponse>(content);
                    var toppings = apiResponse?.Data?.Select(item => new ToppingItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Price = item.RevenueCenters?.FirstOrDefault()?.SellingPrice ?? 0,
                        ItemClassId = item.ItemClass?.Id,
                        TaxId = item.TaxId
                    }).ToList() ?? new List<ToppingItem>();
                    
                    return toppings;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception)
            {
            }

            return new List<ToppingItem>();
        }

        
        private OrderRequest CreateSampleOrder()
        {
      
            OrderRequest sampleOrderRequest = new OrderRequest
            {
                RestaurantId = "f4036dbe-e184-4f64-aa12-10d8f7f7b5a9",
                RevenueCenterId = "cad3420c-c420-4e93-9f4b-b8317fe236a5",
                CreatedById = null, // Maps to 'null' in JSON
                Taxes = new List<TaxRequest>(), // Maps to '[]' in JSON
                Items = new List<OrderItemRequest>
    {
        new OrderItemRequest
        {
            GroupingId = null,
            ItemId = "f81a72ef-5852-4b1c-ad8d-ebdca2e1186c",
            ItemClassId = "d97582d8-e430-4c6f-8932-8ae0ac1077c1",
            ItemPackageId = null,
            ItemParentId = null,
            ItemRecipeId = null,
            Quantity = 1,
            OriginalQuantity = 0,
            Name = "Bread",
            Amount = 100, // decimal type
            Price = 100, // decimal type
            ItemPreparationAreaId = null,
            PkgQty = 0,
            DiscountCode = null,
            DiscountAmount = 0 // decimal type
        }
    },
                Charges = new List<OrderCharge>(), // Maps to '[]' in JSON
                PaymentChannels = new List<PaymentChannel>
    {
        new PaymentChannel
        {
            AmountPaid = 100, // decimal type
            Channel = "ThirdParty"
        }
    },
                CustomChannels = new List<CustomChannel>
    {
        new CustomChannel
        {
            Name = "Sterling bank",
            Description = "Channel For Sterling/Embedly",
            CustomChannelId = "ee34f898-496f-4524-ba09-dee0266efa29",
            Percentage = 1, // decimal type
            Amount = 100 // decimal type
        }
    },
                SourceId = "e059a93c-5423-4d07-a7df-8e48b38c428b",
                Adjustments = "\n\n08037536087",
                PhoneNumber = "+2348172079203",
                CustomerName = "Malik",
                LoyaltyPoints = 0, // int type
                Name = null,
                Email = "malik@food-ease.io",
                Address = "Jubril Ayinla Hostel",
                ServiceType = "Delivery",
                Callback_url = null,
                DiscountCode = null,
                DiscountAmount = 0 // decimal type
            };


            return sampleOrderRequest;
        }


        public async Task<OrderData> CreateOrderAsync(OrderRequest orderRequest, string token)
        {
            using var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", token);

            var response = await client.PostAsJsonAsync("https://api.food-ease.io/api/v1/Order/whatsapp-order",orderRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Order creation failed: {error}");
            }
            var result = await response.Content.ReadFromJsonAsync<OrderResponse>();

            return result?.Data;
        }

        public async Task<List<TaxInfo>> GetTaxesAsync(string restaurantId, string token)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://api.food-ease.io/api/v1/Tax/list-all?restaurantId={restaurantId}");

            if (!response.IsSuccessStatusCode)
            {
                return new List<TaxInfo>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TaxListResponse>(content);

            return result?.Data ?? new List<TaxInfo>();
        }

        public async Task<List<RevenueCenter>> GetRevenueCenters(string businessId)
        {
            var business = await _db.WhatsAppBusinesses.FindAsync(businessId);
            if (business == null)
            {
                return new List<RevenueCenter>();
            }

            using var client = _httpClientFactory.CreateClient();
            var url = $"https://api.food-ease.io/api/v1/RevenueCenter/list-all-by-restaurant?restaurantId={business.RestaurantId}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<RevenueCenter>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RevenueCenterListResponse>(content);

            return result?.Data?.Take(10).ToList() ?? new List<RevenueCenter>();
        }

        public async Task<List<OrderChargeInfo>> GetChargesAsync(string restaurantId, string revenueCenterId, string serviceType)
        {
            using var client = _httpClientFactory.CreateClient();
            var url = $"https://api.food-ease.io/api/v1/OrderCharge/whatsapp-list-charges?restaurantId={restaurantId}&revCenterId={revenueCenterId}&serviceType={serviceType}";
            
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<OrderChargeInfo>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ChargeListResponse>(content);

            return result?.Data?.Data ?? new List<OrderChargeInfo>();
        }

        public async Task<DiscountValidationResponse> ValidateDiscountCodeAsync(string discountCode, string restaurantId)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var url = $"https://api.food-ease.io/api/v1/DiscountCode/ValidateCode?discountCode={discountCode}&restaurantId={restaurantId}";
                
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DiscountValidationResponse>(content);
                }

                return new DiscountValidationResponse { Errors = new List<string> { "Validation request failed." } };
            }
            catch (Exception ex)
            {
                return new DiscountValidationResponse { Errors = new List<string> { ex.Message } };
            }
        }

        public async Task<WhatsAppBusiness> GetBusinessAsync(string businessId)
        {
            return await _db.WhatsAppBusinesses.FindAsync(businessId);
        }

        public async Task<OrderSession> GetOrderSessionAsync(string businessId, string phoneNumber)
        {
            return await _db.OrderSessions
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.PhoneNumber == phoneNumber);
        }

        public void AddOrder(Order order)
        {
            _db.Orders.Add(order);
        }

        public void RemoveOrderSession(OrderSession session)
        {
            _db.OrderSessions.Remove(session);
        }
    }
}
