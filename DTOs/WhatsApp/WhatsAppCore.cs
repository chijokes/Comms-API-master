using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FusionComms.DTOs.WhatsApp
{
    public class WhatsAppMessageDto
    {
        [Required] public string PhoneNumber { get; set; }
        [Required] public string TemplateName { get; set; }
        [Required] public List<string> BodyParameters { get; set; } = new();
        public string MediaId { get; set; }
    }

    public class WhatsAppTextMessageDto
    {
        [Required] public string PhoneNumber { get; set; }
        [Required] public string TextContent { get; set; }
    }

    public class WhatsAppMediaDto
    {
        public string MediaId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Caption { get; set; }
        public string ExpirationDate { get; set; }
    }

    public class WhatsAppMessageStatisticsDto
    {
        public Dictionary<string, double> StatusPercentages { get; set; } = new();
        public int TotalMessages { get; set; }
    }

    public class WhatsAppMessageStatusDto
    {
        public string Content { get; set; }
        public string PhoneNumber { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WhatsAppResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class WhatsAppResponse<T> : WhatsAppResponse
    {
        public T Data { get; set; }
    }

    public class WhatsAppTemplateDto
    {
        public string TemplateName { get; set; }
        public int ParameterCount { get; set; }
        public TemplateBodyDto Body { get; set; }
    }

    public class TemplateBodyDto
    {
        public string Text { get; set; }
        public List<string> Examples { get; set; }
    }

    public class WhatsAppWebhookVerificationRequest
    {
        [FromQuery(Name = "hub.mode")] public string HubMode { get; set; }
        [FromQuery(Name = "hub.challenge")] public string HubChallenge { get; set; }
        [FromQuery(Name = "hub.verify_token")] public string HubVerifyToken { get; set; }
    }

    public class WhatsAppButton
    {
        public string Text { get; set; }
        public string Payload { get; set; }

        public object ToApiObject() => new
        {
            type = "reply",
            reply = new { id = Payload, title = Text }
        };
    }

    public class WhatsAppSection
    {
        public string Title { get; set; }
        public List<WhatsAppRow> Rows { get; set; }
    }

    public class WhatsAppRow
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class FacebookBatchResponseItem
    {
        public int Code { get; set; }
        public string Body { get; set; }
    }

    public class FacebookBatchResponse : List<FacebookBatchResponseItem> { }

    public class FacebookProductResponse
    {
        public string Id { get; set; }
    }

    public class FacebookProductSetResponse
    {
        public string Id { get; set; }
    }
}
