using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FusionComms.DTOs.WhatsApp
{
    public class RevenueCenterListResponse
    {
        public List<RevenueCenter> Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class RevenueCenter
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Restaurant Restaurant { get; set; }
        public string PhoneNumber { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        [JsonProperty("email")] public string HelpEmail { get; set; }
        [JsonProperty("phoneNumber")] public string HelpPhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool Packaging { get; set; }
        public bool PickupAvailable { get; set; }
    }

    public class Restaurant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool TaxExclusive { get; set; }
        [JsonProperty("startTime")][JsonConverter(typeof(IsoDateTimeConverter))] public DateTime StartTime { get; set; }
        [JsonProperty("endTime")][JsonConverter(typeof(IsoDateTimeConverter))] public DateTime EndTime { get; set; }
        [JsonProperty("deliveryEnd")][JsonConverter(typeof(IsoDateTimeConverter))] public DateTime DeliveryEnd { get; set; }
    }

    public class ChargeListResponse
    {
        public ChargeData Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class ChargeData
    {
        public List<OrderChargeInfo> Data { get; set; }
    }

    public class OrderChargeInfo
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("restaurant")] public string Restaurant { get; set; }
        [JsonProperty("chargeTypeId")] public string ChargeTypeId { get; set; }
        [JsonProperty("isActive")] public bool IsActive { get; set; }
        [JsonProperty("expiryDate")][JsonConverter(typeof(IsoDateTimeConverter))] public DateTime ExpiryDate { get; set; }
        [JsonProperty("minAmount")] public decimal MinAmount { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }
        [JsonProperty("revenueCenters")] public List<ChargeRevenueCenter> RevenueCenters { get; set; } = new List<ChargeRevenueCenter>();
        [JsonProperty("chargeServices")] public List<ChargeService> ChargeServices { get; set; } = new List<ChargeService>();
    }

    public class ChargeRevenueCenter
    {
        [JsonProperty("revenueCenterId")] public string RevenueCenterId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }

    public class ChargeService
    {
        [JsonProperty("orderCharge")] public string OrderCharge { get; set; }
        [JsonProperty("serviceType")] public string ServiceType { get; set; }
    }

    public class TaxInfo
    {
        [JsonProperty("id")] public string TaxId { get; set; }
        [JsonProperty("amount")] public decimal Rate { get; set; }
    }

    public class TaxListResponse
    {
        public List<TaxInfo> Data { get; set; }
        public List<string> Errors { get; set; }
    }
}
