using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FusionComms.DTOs.WhatsApp
{
    public static class ConfigurationDetails
    {
        public static IConfiguration Configuration { get; private set; }

        public static void Initialize(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }

    public class WhatsappTemplate
    {
        public string name { get; set; }
        public string category { get; set; }
        public string language { get; set; }
        public List<ITemplateComponent> components { get; set; }
    }

    public class SingleWhatsAppCreateResponse
    {
        public string WABID { get; set; }
        public WhatsAppResponse<string> Response { get;set; }
    }

    public class CreateWhatsappTemplateCommand
    {
        public WhatsappTemplate Template { get; set; }
        public string WhatsAppBusinessId { get; set; }

        public WhatsAppTemplateCreateResponse CreateResponse { get; set; }
        public bool SuccessfulCreation { get; set; } = false;

        private readonly HttpClient _httpClient;
        private readonly string _accessToken;

        public string errorMessage { get; set; }
        private string BaseUrl { get; set; }
        
        public CreateWhatsappTemplateCommand(HttpClient httpClient, string accessToken)
        {
            _httpClient = httpClient;
            _accessToken = accessToken;

            IConfiguration config = ConfigurationDetails.Configuration;

            if (config == null)
            {

                throw new InvalidOperationException("ConfigurationHelper was not initialized. Cannot read config.");
            }
            BaseUrl = config["Meta:BaseUrl"];
            
        }
        public async Task ExecuteAsync()
        {
          

            string apiUrl = $"{BaseUrl}/v22.0/{WhatsAppBusinessId}/message_templates";

            string templateJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                Template,
                Newtonsoft.Json.Formatting.Indented
            );
            var content = new StringContent(templateJson, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);


                var stringResponse = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error Posting to Meta: {stringResponse}");
                }



                CreateResponse = JsonConvert.DeserializeObject<WhatsAppTemplateCreateResponse>(stringResponse);

                

                if (string.IsNullOrWhiteSpace(CreateResponse?.Id))
                {
                    var errorMsg = $"Payment confirmation template ID not found in response. Response: {stringResponse}";
                    Log.Error(errorMsg);
                    errorMessage = errorMsg;
                    SuccessfulCreation = false;
                    return;
                }
                SuccessfulCreation = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                SuccessfulCreation = false;
 
            }
        }
    }
}
