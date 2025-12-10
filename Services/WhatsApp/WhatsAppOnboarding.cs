using FusionComms.Data;
using FusionComms.DTOs.WhatsApp;
using FusionComms.Entities.WhatsApp;
using FusionComms.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Serilog;

namespace FusionComms.Services.WhatsApp
{
    public class WhatsAppOnboardingService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _graphApiBaseUrl = "https://graph.facebook.com";

        public WhatsAppOnboardingService(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _db = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WhatsAppResponse> OnboardRestaurantAsync(WhatsAppOnboardingRequest request)
        {
            try
            {
                var tokenResult = await ExchangeCodeForTokenAsync(request.Code, request.AppId);
                if (!tokenResult.Success)
                {
                    return new WhatsAppResponse { Success = false, Message = $"Failed to exchange code for business token: {tokenResult.Message}" };
                }
                await SaveBusinessRecordAsync(request, tokenResult.Data);

                var webhookResult = await SubscribeToWebhooksAsync(request.WabaId, tokenResult.Data);
                if (!webhookResult.Success)
                {
                    return new WhatsAppResponse { Success = false, Message = $"Failed to subscribe to webhooks: {webhookResult.Message}" };
                }

                var phoneResult = await RegisterPhoneNumberAsync(request.PhoneNumberId, tokenResult.Data);
                if (!phoneResult.Success)
                {
                    return new WhatsAppResponse { Success = false, Message = $"Failed to register phone number: {phoneResult.Message}" };
                }

                var templateResult = await CreateMessageTemplateAsync(request.WabaId, tokenResult.Data, request.BusinessName);
                if (!templateResult.Success)
                {
                    return new WhatsAppResponse { Success = false, Message = $"Failed to create message template: {templateResult.Message}" };
                }
                await SaveTemplateRecordAsync(request.BusinessId, templateResult.Data, request.BusinessName);

                return new WhatsAppResponse { Success = true, Message = "Restaurant onboarded successfully" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WhatsApp onboarding failed for BusinessId: {@BusinessId}", request.BusinessId);
                return new WhatsAppResponse { Success = false, Message = $"Onboarding failed: {ex.Message}" };
            }
        }

        public async Task<WhatsAppResponse> OnboardCinemaAsync(WhatsAppOnboardingRequest request)
        {
            return new WhatsAppResponse { Success = true, Message = "Cinema onboarded successfully" };
        }

        private async Task<WhatsAppResponse<string>> ExchangeCodeForTokenAsync(string code, string appId)
        {
            try
            {
                var appSecret = await GetAppSecretAsync(appId);
                if (string.IsNullOrWhiteSpace(appSecret))
                {
                    var errorMsg = $"App secret not found for AppId: {appId}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var client = _httpClientFactory.CreateClient();
                var url = $"{_graphApiBaseUrl}/v23.0/oauth/access_token?client_id={appId}&client_secret={appSecret}&code={code}";
                
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to exchange code for token. Status: {response.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<WhatsAppTokenResponse>(content);
                var accessToken = result?.AccessToken;
                
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var errorMsg = $"Business token not found in response. Response: {content}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }
                
                return new WhatsAppResponse<string> { Success = true, Data = accessToken };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred while exchanging code for token. AppId: {appId}. Error: {ex.Message}";
                Log.Error(ex, errorMsg);
                return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
            }
        }

        private async Task<WhatsAppResponse> SubscribeToWebhooksAsync(string wabaId, string businessToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_graphApiBaseUrl}/v23.0/{wabaId}/subscribed_apps";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to subscribe to webhooks. Status: {response.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse { Success = false, Message = errorMsg };
                }
                return new WhatsAppResponse { Success = true };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred while subscribing to webhooks. WabaId: {wabaId}. Error: {ex.Message}";
                Log.Error(ex, errorMsg);
                return new WhatsAppResponse { Success = false, Message = errorMsg };
            }
        }

        private async Task<WhatsAppResponse> RegisterPhoneNumberAsync(string phoneNumberId, string businessToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_graphApiBaseUrl}/v23.0/{phoneNumberId}/register";

                var payload = new
                {
                    messaging_product = "whatsapp",
                    pin = "123456"
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to register phone number. Status: {response.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse { Success = false, Message = errorMsg };
                }
                return new WhatsAppResponse { Success = true };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred while registering phone number. PhoneNumberId: {phoneNumberId}. Error: {ex.Message}";
                Log.Error(ex, errorMsg);
                return new WhatsAppResponse { Success = false, Message = errorMsg };
            }
        }

        public async Task<WhatsAppResponse<string>> SetupCatalogAsync(string businessId, string wabaId, string businessToken, string businessName, string phoneNumberId)
        {
            try
            {
                
                var client = _httpClientFactory.CreateClient();

                // Get list of business owned catalogs
                var listCatalogsUrl = $"{_graphApiBaseUrl}/v23.0/{businessId}/owned_product_catalogs";
                var listRequest = new HttpRequestMessage(HttpMethod.Get, listCatalogsUrl);
                listRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var listResponse = await client.SendAsync(listRequest);

                if (!listResponse.IsSuccessStatusCode)
                {
                    var errorContent = await listResponse.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to fetch owned catalogs. Status: {listResponse.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var listResult = await listResponse.Content.ReadAsStringAsync();
                var ownedCatalogs = JsonConvert.DeserializeObject<WhatsAppCatalogListResponse>(listResult);
                var catalogId = ownedCatalogs?.Data != null && ownedCatalogs.Data.Count > 0 ? ownedCatalogs.Data[0]?.Id : null;

                if (string.IsNullOrWhiteSpace(catalogId))
                {
                    var errorMsg = $"No owned catalogs found for BusinessId: {businessId}. Response: {listResult}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                // Get catalogId Connnected to business
                var existingCatalogsUrl = $"{_graphApiBaseUrl}/v20.0/{wabaId}/product_catalogs";
                var existingRequest = new HttpRequestMessage(HttpMethod.Get, existingCatalogsUrl);
                existingRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var existingResponse = await client.SendAsync(existingRequest);

                if (existingResponse.IsSuccessStatusCode)
                {
                    var existingContent = await existingResponse.Content.ReadAsStringAsync();
                    var existingCatalogs = JsonConvert.DeserializeObject<WhatsAppCatalogListResponse>(existingContent);

                    if (existingCatalogs != null)
                    {
                        foreach (var catalog in existingCatalogs.Data)
                        {
                            var oldCatalogId = catalog?.Id;
                            if (!string.IsNullOrWhiteSpace(oldCatalogId))
                            {
                                var unlinkUrl = $"{_graphApiBaseUrl}/v20.0/{wabaId}/product_catalogs?catalog_id={oldCatalogId}&access_token={businessToken}";
                                var unlinkResponse = await client.DeleteAsync(unlinkUrl);
                                
                                if (!unlinkResponse.IsSuccessStatusCode)
                                {
                                    var unlinkError = await unlinkResponse.Content.ReadAsStringAsync();
                                    Log.Warning("Failed to unlink old catalog {OldCatalogId}. Status: {StatusCode}, Error: {Error}", oldCatalogId, unlinkResponse.StatusCode, unlinkError);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var existingError = await existingResponse.Content.ReadAsStringAsync();
                    Log.Warning("Failed to check existing catalogs. Status: {StatusCode}, Error: {Error}", existingResponse.StatusCode, existingError);
                }

                // link response catalog to business
                var linkUrl = $"{_graphApiBaseUrl}/v20.0/{wabaId}/product_catalogs";
                var linkPayload = new { catalog_id = catalogId };
                var linkJson = JsonConvert.SerializeObject(linkPayload);
                var linkContent = new StringContent(linkJson, Encoding.UTF8, "application/json");

                var linkRequest = new HttpRequestMessage(HttpMethod.Post, linkUrl)
                {
                    Content = linkContent
                };
                linkRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var linkResponse = await client.SendAsync(linkRequest);

                if (!linkResponse.IsSuccessStatusCode)
                {
                    var errorContent = await linkResponse.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to link catalog. Status: {linkResponse.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var linkResult = await linkResponse.Content.ReadAsStringAsync();

                // Make catalog visible in WhatsApp
                var visibilityUrl = $"{_graphApiBaseUrl}/v23.0/{phoneNumberId}/whatsapp_commerce_settings?is_catalog_visible=true";

                var visibilityRequest = new HttpRequestMessage(HttpMethod.Post, visibilityUrl);
                visibilityRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);

                var visibilityResponse = await client.SendAsync(visibilityRequest);

                if (!visibilityResponse.IsSuccessStatusCode)
                {
                    var errorContent = await visibilityResponse.Content.ReadAsStringAsync();
                    Log.Warning("Failed to make catalog visible. Status: {StatusCode}, Error: {ErrorContent}", visibilityResponse.StatusCode, errorContent);
                }

                return new WhatsAppResponse<string> { Success = true, Data = catalogId };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred while setting up catalog. BusinessId: {businessId}. Error: {ex.Message}";
                Log.Error(ex, errorMsg);
                return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
            }
        }

        public async Task<WhatsAppResponse<string>> CreateMessageTemplateAsync(string wabaId, string businessToken, string businessName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_graphApiBaseUrl}/v22.0/{wabaId}/message_templates";

                // Create payment confirmation template
                var paymentConfirmationTemplate = new
                {
                    name = WhatsAppTemplateConstants.PaymentConfirmationTemplate,
                    category = "UTILITY",
                    language = "en",
                    components = new object[]
                    {
                        new
                        {
                            type = "BODY",
                            text = $"Hello {{{{{1}}}}},\n\nYour order has been confirmed! \n\n🧾 *Order ID*: {{{{{2}}}}}\n💰 *Total*: {{{{{3}}}}}.\n\nFor support, contact us at: {{{{{4}}}}}\n\nThank you for choosing {businessName}!",
                            example = new
                            {
                                body_text = new string[][]
                                {
                                    new string[] { "Alex Poe", "CDE123", "₦23,000", "08122333444" }
                                }
                            }
                        },
                        new
                        {
                            type = "FOOTER",
                            text = "Powered by Foodease a product of Fusion Intelligence"
                        }
                    }
                };

                // Create order payment template
                var orderPaymentTemplate = new
                {
                    name = WhatsAppTemplateConstants.OrderPaymentTemplate,
                    category = "UTILITY",
                    language = "en",
                    components = new object[]
                    {
                        new
                        {
                            type = "BODY",
                            text = $"✅ Order Received!\n\n💰 Total: {{{{{1}}}}}\n\nPlease proceed to payment using the button below.\n\nAfter payment, you will be updated on your order status.\nSend 'Hi' or 'Hello' to start a new order.",
                            example = new
                            {
                                body_text = new string[][]
                                {
                                    new string[] { "₦193.00" }
                                }
                            }
                        },
                        new
                        {
                            type = "BUTTONS",
                            buttons = new object[]
                            {
                                new
                                {
                                    type = "URL",
                                    text = "Pay Now",
                                    url = $"https://checkout.nomba.com/pay/{{{{{1}}}}}",
                                    example = "https://checkout.nomba.com/pay/938c630b-fb98-47e1-b935-049e28298687"
                                }
                            }
                        }
                    }
                };

                var paymentConfirmationJson = JsonConvert.SerializeObject(paymentConfirmationTemplate);
                var paymentConfirmationContent = new StringContent(paymentConfirmationJson, Encoding.UTF8, "application/json");
                var paymentConfirmationRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = paymentConfirmationContent
                };
                paymentConfirmationRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);
                var paymentConfirmationResponse = await client.SendAsync(paymentConfirmationRequest);

                if (!paymentConfirmationResponse.IsSuccessStatusCode)
                {
                    var errorContent = await paymentConfirmationResponse.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to create payment confirmation template. Status: {paymentConfirmationResponse.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var paymentConfirmationResponseContent = await paymentConfirmationResponse.Content.ReadAsStringAsync();
                var paymentConfirmationResult = JsonConvert.DeserializeObject<WhatsAppTemplateCreateResponse>(paymentConfirmationResponseContent);
                var paymentConfirmationTemplateId = paymentConfirmationResult?.Id;

                if (string.IsNullOrWhiteSpace(paymentConfirmationTemplateId))
                {
                    var errorMsg = $"Payment confirmation template ID not found in response. Response: {paymentConfirmationResponseContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var orderPaymentJson = JsonConvert.SerializeObject(orderPaymentTemplate);
                var orderPaymentContent = new StringContent(orderPaymentJson, Encoding.UTF8, "application/json");
                var orderPaymentRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = orderPaymentContent
                };
                orderPaymentRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", businessToken);
                var orderPaymentResponse = await client.SendAsync(orderPaymentRequest);

                if (!orderPaymentResponse.IsSuccessStatusCode)
                {
                    var errorContent = await orderPaymentResponse.Content.ReadAsStringAsync();
                    var errorMsg = $"Failed to create order payment template. Status: {orderPaymentResponse.StatusCode}, Error: {errorContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                var orderPaymentResponseContent = await orderPaymentResponse.Content.ReadAsStringAsync();
                var orderPaymentResult = JsonConvert.DeserializeObject<WhatsAppTemplateCreateResponse>(orderPaymentResponseContent);
                var orderPaymentTemplateId = orderPaymentResult?.Id;

                if (string.IsNullOrWhiteSpace(orderPaymentTemplateId))
                {
                    var errorMsg = $"Order payment template ID not found in response. Response: {orderPaymentResponseContent}";
                    Log.Error(errorMsg);
                    return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
                }

                return new WhatsAppResponse<string> { Success = true, Data = paymentConfirmationTemplateId };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred while creating message templates. WabaId: {wabaId}. Error: {ex.Message}";
                Log.Error(ex, errorMsg);
                return new WhatsAppResponse<string> { Success = false, Message = errorMsg };
            }
        }

        private async Task SaveBusinessRecordAsync(WhatsAppOnboardingRequest request, string businessToken)
        {
            try
            {
                var business = new WhatsAppBusiness
                {
                    BusinessId = request.BusinessId,
                    PhoneNumberId = request.PhoneNumberId,
                    AccountId = request.WabaId,
                    BusinessName = request.BusinessName,
                    BusinessToken = businessToken,
                    AppId = request.AppId,
                    SourceId = request.SourceId,
                    SupportsChat = true,
                    BusinessType = BusinessType.Restaurant,
                    RestaurantId = request.RestaurantId,
                    CustomChannelId = request.CustomChannelId
                };

                _db.WhatsAppBusinesses.Add(business);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save business record for BusinessId: {@BusinessId}", request.BusinessId);
                throw;
            }
        }

        private async Task SaveTemplateRecordAsync(string businessId, string templateId, string businessName)
        {
            try
            {
                var template = new WhatsAppTemplate
                {
                    TemplateId = templateId,
                    TemplateName = WhatsAppTemplateConstants.PaymentConfirmationTemplate,
                    Category = TemplateCategory.UTILITY,
                    BusinessId = businessId,
                    Status = "PENDING",
                    Language = "en",
                    CreatedAt = DateTime.UtcNow,
                    ParameterCount = 4,
                    BodyText = $"Hello {{1}},\n\nYour order has been confirmed! \n\nOrder ID: {{2}}\nTotal: {{3}}.\n\nFor support, contact us at: {{4}}\n\nThank you for choosing {businessName}!",
                    ExampleBodyText = "Alex Poe, CDE123, ₦23,000, 08122333444"
                };

                _db.WhatsAppTemplates.Add(template);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save template record for BusinessId: {@BusinessId}", businessId);
                throw;
            }
        }

        private async Task<string> GetAppSecretAsync(string appId)
        {
            try
            {
                var appConfig = await _db.WhatsAppAppConfigs
                    .FirstOrDefaultAsync(wac => wac.AppId == appId);

                if (appConfig == null)
                {
                    Log.Error("App configuration not found for AppId: {@AppId}", appId);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(appConfig.AppSecret))
                {
                    Log.Error("App secret is null or empty for AppId: {@AppId}", appId);
                    return null;
                }

                return appConfig.AppSecret;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while retrieving app secret for AppId: {@AppId}", appId);
                return null;
            }
        }
    }
}
