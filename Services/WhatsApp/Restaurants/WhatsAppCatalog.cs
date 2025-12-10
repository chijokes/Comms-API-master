using FusionComms.DTOs.WhatsApp;
using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using FusionComms.Services.WhatsApp;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class WhatsAppCatalogService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WhatsAppOnboardingService _onboardingService;

        public WhatsAppCatalogService(AppDbContext dbContext, IHttpClientFactory httpClientFactory, WhatsAppOnboardingService onboardingService)
        {
            _db = dbContext;
            _httpClientFactory = httpClientFactory;
            _onboardingService = onboardingService;
        }

        public async Task<WhatsAppResponse> AddProductsAsync(string businessId, List<WhatsAppProductDto> products)
        {
            if (products == null || products.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "No products provided" };

            if (products.Count > 50)
                return new WhatsAppResponse { Success = false, Message = "Maximum 50 products per batch" };

            var invalidRetailerId = products.FirstOrDefault(p => string.IsNullOrEmpty(p.RetailerId));
            if (invalidRetailerId != null)
                return new WhatsAppResponse { Success = false, Message = "RetailerId is required for all products" };

            var invalidRevenueCenterIds = products.FirstOrDefault(p => p.RevenueCenterIds == null || p.RevenueCenterIds.Count == 0);
            if (invalidRevenueCenterIds != null)
                return new WhatsAppResponse { Success = false, Message = $"RevenueCenterIds is required for product with RetailerId: {invalidRevenueCenterIds.RetailerId}" };

            var emptyRevenueCenterValues = products.FirstOrDefault(p => p.RevenueCenterIds.Any(string.IsNullOrEmpty));
            if (emptyRevenueCenterValues != null)
                return new WhatsAppResponse { Success = false, Message = $"RevenueCenterIds cannot contain empty values for product with RetailerId: {emptyRevenueCenterValues.RetailerId}" };

            var business = await _db.WhatsAppBusinesses
                .FirstOrDefaultAsync(b => b.BusinessId == businessId);

            if (business == null)
                return new WhatsAppResponse { Success = false, Message = "Business not found" };

            if (string.IsNullOrEmpty(business.CatalogId))
            {
                var setup = await _onboardingService.SetupCatalogAsync(business.BusinessId, business.AccountId, business.BusinessToken, business.BusinessName, business.PhoneNumberId);
                if (!setup.Success)
                    return new WhatsAppResponse { Success = false, Message = $"Failed to setup catalog: {setup.Message}" };

                business.CatalogId = setup.Data;
                await _db.SaveChangesAsync();
            }

            var retailerIds = products.Select(p => p.RetailerId).Distinct().ToList();
            var existingRetailerIds = await _db.WhatsAppProducts
                .Where(p => retailerIds.Contains(p.RetailerId))
                .Select(p => p.RetailerId)
                .Distinct()
                .ToListAsync();

            if (existingRetailerIds.Count > 0)
            {
                var duplicateIds = string.Join(", ", existingRetailerIds);
                return new WhatsAppResponse { Success = false, Message = $"Products with retailer IDs already exist: {duplicateIds}" };
            }

            var accessToken = business.BusinessToken;
            var catalogId = business.CatalogId;
            var batchOperations = new List<object>();
            var categories = new HashSet<string>();

            var productGroups = products.GroupBy(p => p.RetailerId).ToList();

            foreach (var productGroup in productGroups)
            {
                var firstProduct = productGroup.First();
                
                if (string.IsNullOrEmpty(firstProduct.RetailerId))
                    return new WhatsAppResponse { Success = false, Message = "RetailerId is required" };

                var formData = new Dictionary<string, string>
                {
                    {"name", firstProduct.Name},
                    {"retailer_id", firstProduct.RetailerId},
                    {"price", ((int)Math.Round(firstProduct.Price * 100)).ToString()},
                    {"description", firstProduct.Description},
                    {"image_url", firstProduct.ImageUrl},
                    {"currency", firstProduct.Currency},
                    {"availability", firstProduct.Availability},
                    {"condition", firstProduct.Condition},
                    {"status", firstProduct.Status},
                    {"category", firstProduct.Category}
                };

                if (firstProduct.SalePrice.HasValue)
                    formData.Add("sale_price", ((int)Math.Round(firstProduct.SalePrice.Value * 100)).ToString());

                var body = string.Join("&", formData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));


                batchOperations.Add(new
                {
                    method = "POST",
                    relative_url = $"{catalogId}/products",
                    body
                });

                if (!string.IsNullOrEmpty(firstProduct.Category))
                    categories.Add(firstProduct.Category);
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://graph.facebook.com/v22.0/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var batchRequest = new { batch = batchOperations };
            var content = new StringContent(JsonConvert.SerializeObject(batchRequest), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new WhatsAppResponse { Success = false, Message = $"Facebook API error: {error}" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var batchResponse = JsonConvert.DeserializeObject<FacebookBatchResponse>(responseContent);

            var addedProducts = new List<WhatsAppProduct>();
            var productGroupIndex = 0;

            foreach (var productGroup in productGroups)
            {
                if (batchResponse[productGroupIndex].Code >= 200 && batchResponse[productGroupIndex].Code < 300)
                {
                    var productResponse = JsonConvert.DeserializeObject<FacebookProductResponse>(batchResponse[productGroupIndex].Body);
                    var firstProduct = productGroup.First();

                    var revenueCenterIds = productGroup.SelectMany(p => p.RevenueCenterIds).Distinct().ToList();

                    foreach (var revenueCenterId in revenueCenterIds)
                    {
                        var localProduct = new WhatsAppProduct
                        {
                            ProductId = productResponse.Id,
                            RetailerId = firstProduct.RetailerId,
                            Name = firstProduct.Name,
                            Category = firstProduct.Category,
                            Subcategory = firstProduct.Subcategory,
                            RevenueCenterId = revenueCenterId,
                            CreatedAt = DateTime.UtcNow,
                        };

                        addedProducts.Add(localProduct);
                    }
                }
                productGroupIndex++;
            }

            var categoryProductSets = new Dictionary<string, WhatsAppProductSet>();
            
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category))
                {
                    var productSet = await _db.WhatsAppProductSets
                        .FirstOrDefaultAsync(ps => 
                            ps.BusinessId == businessId && 
                            ps.Name == category);

                    if (productSet != null)
                    {
                    }
                    else
                    {
                        productSet = await CreateProductSetForFilter(
                                businessId,
                                catalogId,
                                accessToken,
                                "category",
                                category
                            );
                    }

                    if (productSet != null)
                    {
                        categoryProductSets[category] = productSet;
                    }
                }
            }

            foreach (var product in addedProducts)
            {
                if (categoryProductSets.TryGetValue(product.Category, out var productSet))
                {
                    product.ProductSet = productSet;
                    product.SetId = productSet.SetId;
                }
            }

            if (addedProducts.Count > 0)
            {
                await _db.WhatsAppProducts.AddRangeAsync(addedProducts);
                var saveResult = await _db.SaveChangesAsync();
                
                
                var savedCount = await _db.WhatsAppProducts
                    .Where(p => addedProducts.Select(ap => ap.RetailerId).Contains(p.RetailerId))
                    .CountAsync();
            }

            return new WhatsAppResponse { Success = true, Message = "Products added successfully" };
        }

        public async Task<WhatsAppResponse> UpdateProductAsync(string businessId, string retailerId, WhatsAppProductUpdateDto update)
        {
            var business = await _db.WhatsAppBusinesses
                .FirstOrDefaultAsync(b => b.BusinessId == businessId);

            if (business == null)
                return new WhatsAppResponse { Success = false, Message = "Business not found" };

            if (update.RevenueCenterIds != null)
            {
                var emptyRevenueCenterValue = update.RevenueCenterIds.FirstOrDefault(string.IsNullOrEmpty);
                if (emptyRevenueCenterValue != null)
                    return new WhatsAppResponse { Success = false, Message = "RevenueCenterIds cannot contain empty values" };
            }

            var localProducts = await _db.WhatsAppProducts
                .Where(p => p.RetailerId == retailerId)
                .ToListAsync();

            if (localProducts.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "Product not found" };

            var businessProducts = localProducts
                .Where(p => !string.IsNullOrEmpty(p.SetId))
                .ToList();

            var validBusinessProducts = new List<WhatsAppProduct>();

            if (businessProducts.Count > 0)
            {
                foreach (var product in businessProducts)
                {
                    var productSet = await _db.WhatsAppProductSets
                        .FirstOrDefaultAsync(ps => ps.SetId == product.SetId && ps.BusinessId == businessId);
                    
                    if (productSet != null)
                        validBusinessProducts.Add(product);
                }
            }

            if (validBusinessProducts.Count == 0)
                validBusinessProducts = localProducts.ToList();

            if (validBusinessProducts.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "Product not found" };

            var metaProduct = validBusinessProducts.FirstOrDefault(p => p.RevenueCenterId == "default");
            if (metaProduct == null)
                metaProduct = validBusinessProducts.First();

            var metaProductId = metaProduct.ProductId;

            if (update.RevenueCenterIds != null && update.RevenueCenterIds.Count > 0)
            {
                var currentRevenueCenterIds = validBusinessProducts
                    .Where(p => p.RevenueCenterId != "default")
                    .Select(p => p.RevenueCenterId)
                    .Distinct()
                    .ToList();

                var newRevenueCenterIds = update.RevenueCenterIds.Distinct().ToList();

                var revenueCentersToRemove = currentRevenueCenterIds.Except(newRevenueCenterIds).ToList();
                if (revenueCentersToRemove.Count > 0)
                {
                    var productsToRemove = validBusinessProducts.Where(p => revenueCentersToRemove.Contains(p.RevenueCenterId)).ToList();
                    _db.WhatsAppProducts.RemoveRange(productsToRemove);
                }

                var revenueCentersToAdd = newRevenueCenterIds.Except(currentRevenueCenterIds).ToList();
                foreach (var revenueCenterId in revenueCentersToAdd)
                {
                    var newLocalProduct = new WhatsAppProduct
                    {
                        ProductId = metaProductId,
                        RetailerId = retailerId,
                        Name = update.Name ?? metaProduct.Name,
                        Category = update.Category ?? metaProduct.Category,
                        Subcategory = update.Subcategory ?? metaProduct.Subcategory,
                        RevenueCenterId = revenueCenterId,
                        CreatedAt = DateTime.UtcNow,
                        SetId = metaProduct.SetId
                    };
                    _db.WhatsAppProducts.Add(newLocalProduct);
                }

                var productsToUpdate = validBusinessProducts.Where(p => newRevenueCenterIds.Contains(p.RevenueCenterId)).ToList();
                foreach (var product in productsToUpdate)
                {
                    if (!string.IsNullOrEmpty(update.Name))
                        product.Name = update.Name;
                    if (!string.IsNullOrEmpty(update.Category))
                        product.Category = update.Category;
                    if (!string.IsNullOrEmpty(update.Subcategory))
                        product.Subcategory = update.Subcategory;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            var hasNonRevenueCenterChanges = !string.IsNullOrEmpty(update.Name) || 
                                        update.Price.HasValue || 
                                        !string.IsNullOrEmpty(update.Currency) ||
                                        update.SalePrice.HasValue ||
                                        !string.IsNullOrEmpty(update.Description) ||
                                        !string.IsNullOrEmpty(update.ImageUrl) ||
                                        !string.IsNullOrEmpty(update.Condition) ||
                                        !string.IsNullOrEmpty(update.Availability) ||
                                        !string.IsNullOrEmpty(update.Status) ||
                                        !string.IsNullOrEmpty(update.Category);

            if (hasNonRevenueCenterChanges)
            {
                var payload = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(update.Name))
                    payload["name"] = update.Name;

                if (update.Price.HasValue)
                    payload["price"] = (int)Math.Round(update.Price.Value * 100);

                if (!string.IsNullOrEmpty(update.Currency))
                    payload["currency"] = update.Currency;

                if (update.SalePrice.HasValue)
                    payload["sale_price"] = (int)Math.Round(update.SalePrice.Value * 100);

                if (!string.IsNullOrEmpty(update.Description))
                    payload["description"] = update.Description;

                if (!string.IsNullOrEmpty(update.ImageUrl))
                    payload["image_url"] = update.ImageUrl;

                if (!string.IsNullOrEmpty(update.Condition))
                    payload["condition"] = update.Condition;

                if (!string.IsNullOrEmpty(update.Availability))
                    payload["availability"] = update.Availability;

                if (!string.IsNullOrEmpty(update.Status))
                    payload["status"] = update.Status;

                if (!string.IsNullOrEmpty(update.Category))
                    payload["category"] = update.Category;

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://graph.facebook.com/v22.0/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.BusinessToken);

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(metaProductId, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new WhatsAppResponse { Success = false, Message = $"Facebook API error: {error}" };
                }

                foreach (var product in validBusinessProducts)
                {
                    if (!string.IsNullOrEmpty(update.Name))
                        product.Name = update.Name;
                    if (!string.IsNullOrEmpty(update.Category))
                        product.Category = update.Category;
                    if (!string.IsNullOrEmpty(update.Subcategory))
                        product.Subcategory = update.Subcategory;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (!string.IsNullOrEmpty(update.Category))
            {
                var newProductSet = await _db.WhatsAppProductSets
                    .FirstOrDefaultAsync(ps => 
                        ps.BusinessId == businessId && 
                        ps.Name == update.Category);

                if (newProductSet == null)
                {
                    newProductSet = await CreateProductSetForFilter(
                            businessId,
                            business.CatalogId,
                            business.BusinessToken,
                            "category",
                            update.Category
                        );
                }

                if (newProductSet != null)
                {
                    foreach (var product in validBusinessProducts)
                    {
                        product.ProductSet = newProductSet;
                        product.SetId = newProductSet.SetId;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return new WhatsAppResponse { Success = true, Message = "Product updated successfully" };
        }

        public async Task<WhatsAppResponse> DeleteProductsAsync(string businessId, List<string> retailerIds)
        {
            if (retailerIds == null || retailerIds.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "No product IDs provided" };

            if (retailerIds.Count > 50)
                return new WhatsAppResponse { Success = false, Message = "Maximum 50 products per batch" };

            var business = await _db.WhatsAppBusinesses
                .FirstOrDefaultAsync(b => b.BusinessId == businessId);

            if (business == null)
                return new WhatsAppResponse { Success = false, Message = "Business not found" };

            var existingProducts = await _db.WhatsAppProducts
                .Where(p => retailerIds.Contains(p.RetailerId))
                .ToListAsync();

            if (existingProducts.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "No matching products found" };

            var productsToDelete = existingProducts
                .Where(p => !string.IsNullOrEmpty(p.ProductId))
                .ToList();

            if (productsToDelete.Count == 0)
                return new WhatsAppResponse { Success = false, Message = "No matching products found" };

            var uniqueMetaProductIds = productsToDelete
                .Select(p => p.ProductId)
                .Where(pid => !string.IsNullOrEmpty(pid))
                .Distinct()
                .ToList();

            var batchOperations = uniqueMetaProductIds.Select(productId => new
            {
                method = "DELETE",
                relative_url = productId
            }).ToList();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://graph.facebook.com/v22.0/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.BusinessToken);

            var batchRequest = new { batch = batchOperations };
            var content = new StringContent(JsonConvert.SerializeObject(batchRequest), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new WhatsAppResponse { Success = false, Message = $"Facebook API error: {error}" };
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            _db.WhatsAppProducts.RemoveRange(productsToDelete);
            var saveResult = await _db.SaveChangesAsync();
            
            return new WhatsAppResponse { Success = true, Message = "Products deleted successfully" };
        }

        private async Task<WhatsAppProductSet> CreateProductSetForFilter(
            string businessId,
            string catalogId,
            string accessToken,
            string filterField,
            string filterValue)
        {
            var existingSet = await _db.WhatsAppProductSets
                .FirstOrDefaultAsync(ps =>
                    ps.BusinessId == businessId &&
                    ps.Name == filterValue);

            if (existingSet != null)
                return existingSet;

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://graph.facebook.com/v22.0/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var request = new
            {
                name = filterValue,
                filter = new Dictionary<string, object>
                {
                    [filterField] = new { eq = filterValue }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{catalogId}/product_sets", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var setResponse = JsonConvert.DeserializeObject<FacebookProductSetResponse>(responseContent);

            var newSet = new WhatsAppProductSet
            {
                SetId = setResponse.Id,
                CatalogId = catalogId,
                Name = filterValue,
                CreatedAt = DateTime.UtcNow,
                BusinessId  = businessId
            };

            await _db.WhatsAppProductSets.AddAsync(newSet);
            var saveResult = await _db.SaveChangesAsync();
            
            return newSet;
        }

        public async Task<WhatsAppResponse> UpdateFeaturedProductsAsync(string businessId, List<string> retailerIds)
        {
            try
            {
                var businessProducts = await _db.WhatsAppProducts
                    .Where(p => p.ProductSet.BusinessId == businessId)
                    .ToListAsync();

                foreach (var product in businessProducts)
                {
                    product.IsFeatured = false;
                }

                if (retailerIds.Count > 0)
                {
                    var existingRetailerIds = new HashSet<string>(
                        businessProducts.Select(p => p.RetailerId));

                    var invalidRetailerIds = retailerIds
                        .Where(id => !existingRetailerIds.Contains(id))
                        .ToList();

                    if (invalidRetailerIds.Count > 0)
                    {
                        var invalidIdsString = string.Join(", ", invalidRetailerIds);
                        return new WhatsAppResponse 
                        { 
                            Success = false, 
                            Message = $"The following retailer IDs do not exist in the database: {invalidIdsString}" 
                        };
                    }

                    var productsToFeature = businessProducts
                        .Where(p => retailerIds.Contains(p.RetailerId))
                        .ToList();

                    foreach (var product in productsToFeature)
                    {
                        product.IsFeatured = true;
                    }
                }

                await _db.SaveChangesAsync();

                var message = (retailerIds != null && retailerIds.Count > 0)
                    ? $"Successfully updated featured products. {retailerIds.Count} retailer IDs are now featured."
                    : "Successfully removed all products from featured status.";

                return new WhatsAppResponse { Success = true, Message = message };
            }
            catch (Exception ex)
            {
                return new WhatsAppResponse { Success = false, Message = $"Error updating featured products: {ex.Message}" };
            }
        }
    }
}