using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FusionComms.DTOs.WhatsApp
{
    public class OrderRequest
    {
        public string RestaurantId { get; set; }
        public string RevenueCenterId { get; set; }
        public string CreatedById { get; set; } = null;
        public List<TaxRequest> Taxes { get; set; } = new();
        public List<OrderItemRequest> Items { get; set; } = new();
        public List<OrderCharge> Charges { get; set; } = new();
        public List<PaymentChannel> PaymentChannels { get; set; } = new();
        public List<CustomChannel> CustomChannels { get; set; } = new();
        public string SourceId { get; set; }
        public string Adjustments { get; set; } = null;
        public string PhoneNumber { get; set; }
        public string CustomerName { get; set; }
        public int LoyaltyPoints { get; set; } = 0;
        public string Name { get; set; } = null;
        public string Email { get; set; }
        public string Address { get; set; }
        public string ServiceType { get; set; }
        public string Callback_url { get; set; } = null;
        public string DiscountCode { get; set; } = null;
        public decimal DiscountAmount { get; set; } = 0;
    }

    public class TaxRequest
    {
        public string TaxId { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
    }

    public class OrderCharge
    {
        public string OrderChargeId { get; set; }
    }

    public class PaymentChannel
    {
        public decimal AmountPaid { get; set; }
        public string Channel { get; set; }
    }

    public class CustomChannel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CustomChannelId { get; set; }
        public decimal Percentage { get; set; }
        public decimal Amount { get; set; }
    }

    public class OrderItemRequest
    {
        public string GroupingId { get; set; } = null;
        public string ItemId { get; set; }
        public string ItemClassId { get; set; }
        public string ItemPackageId { get; set; } = null;
        public string ItemParentId { get; set; } = null;
        public string ItemRecipeId { get; set; } = null;
        public int Quantity { get; set; }
        public int OriginalQuantity { get; set; } = 0;
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public string ItemPreparationAreaId { get; set; } = null;
        public int PkgQty { get; set; } = 0;
        public string DiscountCode { get; set; } = null;
        public decimal DiscountAmount { get; set; } = 0;
    }

    public class OrderResponse
    {
        public OrderData Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class OrderData
    {
        public string OrderId { get; set; }
        public string Message { get; set; }
        public string CheckoutLink { get; set; }
        public string OrderReference { get; set; }
        public string paymentAccount { get; set; }
        public string bankName { get; set; }
        public string accountName { get; set; }
    }

    public class OrderCart
    {
        public List<CartItem> Items { get; set; } = new();
    }

    public class CartItem
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ItemClassId { get; set; }
        public string TaxId { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsParentItem { get; set; }
        public string ParentItemId { get; set; }
        public string GroupingId { get; set; }
        public string PackId { get; set; } = "pack1";
        public bool IsTopping { get; set; }
        public string MainItemId { get; set; }
    }

    public class WhatsAppOrderItem
    {
        public string Product_retailer_id { get; set; }
        public int Quantity { get; set; }
        public decimal Item_price { get; set; }
    }
}
