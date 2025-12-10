using FusionComms.Data;
using FusionComms.DTOs;
using FusionComms.DTOs.WhatsApp;
using FusionComms.Entities.WhatsApp;
using FusionComms.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static FusionComms.DTOs.WhatsApp.CreateBodyComponent;
using static FusionComms.Services.WhatsApp.WhatsAppTemplateService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FusionComms.Services.WhatsApp
{
    public enum WhatsappTemplateConstants
    {
        OrderPaymentV2Template,
        OrderPaymentV3Template
    }

    

    public interface IWhatsAppTemplateService
    {
        public Task<WhatsAppResponse<string>> CreateTemplate(WhatsappTemplateConstants functionId, string waBaaId, string businessToken);
        public Task<List<SingleWhatsAppCreateResponse>> CreateTemplatesForAll(WhatsappTemplateConstants functionId);

    }

    public class WhatsAppTemplateService : IWhatsAppTemplateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly string _graphApiBaseUrl;
        private readonly AppDbContext _db;

        public WhatsAppTemplateService(IHttpClientFactory httpClientFactory, AppDbContext db, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _config = config;
            _graphApiBaseUrl = config["Meta:BaseUrl"];

            if (string.IsNullOrEmpty(_graphApiBaseUrl))
            {
                throw new InvalidOperationException("Configuration setting 'Meta:BaseUrl' is missing.");
            }

        }

        private string ResolveCategory(string template)
        {
            switch (template)
            {
                case WhatsAppTemplateConstants.OrderPaymentTemplateV3:
                    return "MARKETING";
                default:
                    return null;
            }
        }



        public async Task<WhatsAppResponse<string>> CreateTemplate(WhatsappTemplateConstants template, string waBaaId, string businessToken)
        {
            Func<string, string, Task<WhatsAppResponse<string>>> targetFunction;
            switch (template)
            {
                case WhatsappTemplateConstants.OrderPaymentV2Template:
                    targetFunction = CreateOrderPaymentV2Template;
                    break;
                case WhatsappTemplateConstants.OrderPaymentV3Template:
                    targetFunction = CreateOrderPaymentV3Template;
                    break;
                default:
                    return new WhatsAppResponse<string>() { Message = "Invalid Function Id", Success = false };       
                        
            }

            return await targetFunction.Invoke(waBaaId, businessToken);

        }

        public async Task<List<SingleWhatsAppCreateResponse>> CreateTemplatesForAll(WhatsappTemplateConstants template)
        {
            Func<string, string, Task<WhatsAppResponse<string>>> targetFunction;
            switch (template)
            {
                case WhatsappTemplateConstants.OrderPaymentV2Template:
                    targetFunction = CreateOrderPaymentV2Template;
                    break;
                case WhatsappTemplateConstants.OrderPaymentV3Template:
                    targetFunction = CreateOrderPaymentV3Template;
                    break;
                default:
                    return new List<SingleWhatsAppCreateResponse>() { };
            }

            var accessTokens = await _db.WhatsAppAppConfigs.ToListAsync();

            var businessData = await _db.WhatsAppBusinesses.Select(c => new { c.AccountId, c.BusinessToken }).ToListAsync();

            if (businessData.Count < 1)
            {
                return new List<SingleWhatsAppCreateResponse>() { };
            }

            var results = new List<SingleWhatsAppCreateResponse>();
            foreach (var businessDatum in businessData)
            {
               if (string.IsNullOrWhiteSpace(businessDatum.BusinessToken)) { results.Add(new SingleWhatsAppCreateResponse() { WABID = businessDatum.AccountId, Response = new WhatsAppResponse<string>() { Success = false,Data = "No Access token found" } }); continue; }
                var response = await targetFunction.Invoke(businessDatum.AccountId, businessDatum.BusinessToken);
                results.Add(new SingleWhatsAppCreateResponse() { WABID = businessDatum.AccountId, Response = response });

            }
            return results;


        }


        #region OrderPaymentTemplateV2
        private static WhatsappTemplate GetOrderPaymentV2Template()
        {
            var body = new CreateBodyComponent($"✅ Order Received!\n\n💰 Total: {{{{{1}}}}}\n\nPlease proceed to payment by transferring to the Account Above.\n\nAfter payment, you will be updated on your order status.\nSend 'Hi' or 'Hello' to start a new order.", new List<string> { "₦193.00" });
            var copyCodeButton = new CreateCopyCodeButton()
            {
                example = "6856963932",
            };
            var buttonsComponent = new ButtonsComponent();
            buttonsComponent.buttons.Add(copyCodeButton);
            var components = new List<ITemplateComponent>() { body, buttonsComponent };

            var OrderPaymentTemplate = new WhatsappTemplate()
            {
                name = WhatsAppTemplateConstants.OrderPaymentTemplateV2,
                category = "UTILITY",
                language = "en",
                components = components
            };
            return OrderPaymentTemplate;
        }

        private async Task<WhatsAppResponse<string>> CreateOrderPaymentV2Template(string wabaId, string businessToken)
        {
            var OrderPaymentTemplate = GetOrderPaymentV2Template();
            var client = _httpClientFactory.CreateClient();
            var createOrderPaymentTemplate = new CreateWhatsappTemplateCommand(client, businessToken) { Template = OrderPaymentTemplate, WhatsAppBusinessId = wabaId };

            await createOrderPaymentTemplate.ExecuteAsync();

            if (!createOrderPaymentTemplate.SuccessfulCreation)
            {
                return new WhatsAppResponse<string>() { Message = createOrderPaymentTemplate.errorMessage, Success = false };
            }

            var templateId = createOrderPaymentTemplate.CreateResponse.Id;
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return new WhatsAppResponse<string>() { Message = $"Payment confirmation template ID not found in response." };
            }

            return new WhatsAppResponse<string>() { Data = templateId, Success = true };
        }
        #endregion
        #region OrderOaymentTemplateV3

        private static readonly string OrderPaymentV3TemplateBody = 
            $"✅ Order Received!\n\n" +
                $"💰 Total: {{{{{1}}}}}\n\n" +
                $"Please proceed to payment by transferring to this Account.\n" +
                $"Bank Name: {{{{{2}}}}}\n" +
                $"Account Number: {{{{{3}}}}}\n" +
                $"Account Name: {{{{{4}}}}}\n\n" +
                "Click on Copy Code to Copy the Account Number and paste into your Bank App.\n\n" +
                "After payment, you will be updated on your order status.\nSend 'Hi' or 'Hello' to start a new order.";
        private static WhatsappTemplate GetCreateOrderPaymentV3Template()
        {
            var output = OrderPaymentV3TemplateBody;
            var examples = new List<string>() { "₦193.00" , "Sterling Bank", "6856963932","FoodDeck Limited" };

            var body = new CreateBodyComponent(output,examples);


            var copyCodeButton = new CreateCopyCodeButton()
            {
                example = "6856963932",
            };
            var buttonsComponent = new ButtonsComponent();
            buttonsComponent.buttons.Add(copyCodeButton);
            var components = new List<ITemplateComponent>() { body, buttonsComponent };

            var OrderPaymentTemplate = new WhatsappTemplate()
            {
                name = WhatsAppTemplateConstants.OrderPaymentTemplateV3,
                category = "UTILITY",
                language = "en",
                components = components
            };
            return OrderPaymentTemplate;


        }

        private async Task<WhatsAppResponse<string>> CreateOrderPaymentV3Template(string wabaId, string businessToken)
        {
            var OrderPaymentTemplate = GetCreateOrderPaymentV3Template();
            var client = _httpClientFactory.CreateClient();
            var createOrderPaymentTemplate = new CreateWhatsappTemplateCommand(client, businessToken) { Template = OrderPaymentTemplate, WhatsAppBusinessId = wabaId };

            await createOrderPaymentTemplate.ExecuteAsync();

            if (!createOrderPaymentTemplate.SuccessfulCreation)
            {
                return new WhatsAppResponse<string>() { Message = createOrderPaymentTemplate.errorMessage, Success = false };
            }

            var templateId = createOrderPaymentTemplate.CreateResponse.Id;
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return new WhatsAppResponse<string>() { Message = $"Payment confirmation template ID not found in response." };
            }

            return new WhatsAppResponse<string>() { Data = templateId, Success = true };
        }
        #endregion


    }
}
