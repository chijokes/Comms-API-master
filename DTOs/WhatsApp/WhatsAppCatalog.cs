using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FusionComms.DTOs.WhatsApp
{
    public class CatalogApiResponse
    {
        public List<CatalogItem> Data { get; set; }
    }

    public class CatalogItem
    {
        [Key] public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ItemClassId { get; set; }
        public string TaxId { get; set; }
        public bool HasRecipeParents { get; set; }
        public bool RequiresTopping { get; set; }
        public string ToppingClassId { get; set; }
        public List<ComboOption> ComboOptions { get; set; } = new List<ComboOption>();
        public List<RecipeParent> RecipeParents { get; set; } = new List<RecipeParent>();
        public List<ItemRevenueCenter> RevenueCenters { get; set; } = new List<ItemRevenueCenter>();
    }

    public class ComboOption
    {
        [Key] public string PartnerId { get; set; }
        public string Name { get; set; }
        public decimal ComboPrice { get; set; }
    }

    public class ToppingItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ItemClassId { get; set; }
        public string TaxId { get; set; }
    }

    public class RecipeParent
    {
        [Key] public string Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        [JsonProperty("itemParent")] public ItemParent ItemParent { get; set; }
    }

    public class ToppingListResponse
    {
        public List<ToppingApiItem> Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class ToppingApiItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ToppingRevenueCenter> RevenueCenters { get; set; }
        public ToppingItemClass ItemClass { get; set; }
        public string TaxId { get; set; }
    }

    public class ToppingRevenueCenter
    {
        public decimal SellingPrice { get; set; }
    }

    public class ToppingItemClass
    {
        public string Id { get; set; }
    }

    public class PendingToppings
    {
        public string MainItemId { get; set; }
        public string MainItemName { get; set; }
        public string GroupingId { get; set; }
        public List<ToppingItem> Toppings { get; set; } = new List<ToppingItem>();
        public List<string> SelectedToppingIds { get; set; } = new List<string>();
    }

    public class ItemParent
    {
        [Key] public string Id { get; set; }
        [JsonProperty("itemsInParent")] public List<RecipeItem> ItemsInParent { get; set; } = new List<RecipeItem>();
    }

    public class ItemRevenueCenter
    {
        public string ItemId { get; set; }
        public string RevenueCenterId { get; set; }
        public string RevenueCenter { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class DiscountData
    {
        public string Code { get; set; }
        public decimal Value { get; set; }
        public decimal Amount { get; set; }
        public string DiscountUseType { get; set; }
        public bool IsActive { get; set; }
    }

    public class DiscountValidationResponse
    {
        public DiscountData Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class RecipeItem
    {
        [Key] public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemClassId { get; set; }
    }

    public class PendingParent
    {
        public CatalogItem ParentItem { get; set; }
        public RecipeParent RecipeParent { get; set; }
        public int Quantity { get; set; }
        public int CurrentOptionIndex { get; set; } = 1;
        public int OptionSetIndex { get; set; }
        public string ItemParentId { get; set; }
        public int TotalOptionSets { get; set; }
        public string GroupingId { get; set; }
        public bool HasToppings { get; set; }
        public string ToppingClassId { get; set; }
    }
}
