using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FusionComms.DTOs;
using Serilog;

namespace FusionComms.Services
{
    public interface IZeptoMailService
    {
        Task<(bool Status, string Message)> SendEmail(SendEmailViaZepto model);
    }
    
    
    public class ZeptoMailService : IZeptoMailService
    {
        private readonly IHttpClientFactory _clientFactory;

        public ZeptoMailService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }
        
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public async Task<(bool Status, string Message)> SendEmail(SendEmailViaZepto model)
        {
            Log.Information("Incoming request to send email via zepto using the model: {@Model}", model);
            var httpClient = _clientFactory.CreateClient("zepto");

            Log.Information("sending request to zepto");
            var response = await httpClient.PostAsJsonAsync("/v1.1/email", model);

            Log.Information("Request completed successfully");
            var responseAsString = await response.Content.ReadAsStringAsync(default);

            Log.Information("response as string: {@ResponseAsString}", responseAsString);
            
            var deserializedResponse = JsonSerializer.Deserialize<ZeptoMailResponse>(responseAsString, options);
            Log.Information("deserialized response: {@DeserializedResponse}", deserializedResponse);
            
            return (response.IsSuccessStatusCode, deserializedResponse.Error is null ? deserializedResponse.Message : deserializedResponse.Error.Details.First().Message);
        }
    }
}