using FusionComms.DTOs.WhatsApp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FusionComms.Services.WhatsApp.Restaurants
{
    public class OrderCartManager
    {
        public OrderCart DeserializeCart(string cartData)
        {
            return JsonConvert.DeserializeObject<OrderCart>(cartData ?? "{}") ?? new OrderCart();
        }

        public string SerializeCart(OrderCart cart)
        {
            return JsonConvert.SerializeObject(cart);
        }

        public void AddItemToCart(OrderCart cart, CartItem item)
        {
            cart.Items.Add(item);
        }

        public void RemoveItemsByGroupId(OrderCart cart, string groupingId)
        {
            cart.Items.RemoveAll(i => i.GroupingId == groupingId);
        }

        public List<CartItem> GetChildrenByGroupId(OrderCart cart, string groupingId)
        {
            return cart.Items
                .Where(i => i.GroupingId == groupingId && !string.IsNullOrEmpty(i.ParentItemId))
                .ToList();
        }

        public void UpdateChildrenGroupId(List<CartItem> children, string newGroupingId)
        {
            foreach (var child in children)
            {
                child.GroupingId = newGroupingId;
            }
        }

        public void ClearChildren(List<CartItem> children)
        {
            children.Clear();
        }

        public decimal CalculateSubtotal(OrderCart cart)
        {
            return Math.Round(cart.Items.Sum(i => i.Price * i.Quantity), 2, MidpointRounding.AwayFromZero);
        }

        public List<(string GroupId, string ItemId, string Name, int Quantity, string PackId)> GetItemListForEditing(OrderCart cart, string packId = null)
        {
            var itemList = new List<(string GroupId, string ItemId, string Name, int Quantity, string PackId)>();
            var processedGroups = new HashSet<string>();

            var itemsToProcess = string.IsNullOrEmpty(packId) 
                ? cart.Items 
                : cart.Items.Where(i => i.PackId == packId);

            var comboGroups = itemsToProcess
                .Where(i => !string.IsNullOrEmpty(i.GroupingId))
                .GroupBy(i => i.GroupingId);

            foreach (var group in comboGroups)
            {
                if (processedGroups.Contains(group.Key)) continue;
                processedGroups.Add(group.Key);

                var parent = group.FirstOrDefault(i => string.IsNullOrEmpty(i.ParentItemId));
                if (parent != null)
                {
                    itemList.Add((group.Key, parent.ItemId, parent.Name, parent.Quantity, parent.PackId));
                }
            }

            var standaloneItems = itemsToProcess
                .Where(i => string.IsNullOrEmpty(i.GroupingId) && string.IsNullOrEmpty(i.ParentItemId))
                .GroupBy(i => new { i.ItemId, i.PackId });

            foreach (var group in standaloneItems)
            {
                var item = group.First();
                var quantity = group.Sum(i => i.Quantity);
                itemList.Add((item.ItemId, item.ItemId, item.Name, quantity, item.PackId));
            }

            return itemList;
        }

        public void RemoveItemByNumber(OrderCart cart, int itemNumber, string packId = null)
        {
            var itemList = GetItemListForEditing(cart, packId);
            
            if (itemNumber > 0 && itemNumber <= itemList.Count)
            {
                var (GroupId, ItemId, Name, Quantity, ItemPackId) = itemList[itemNumber - 1];
                
                var targetPackId = packId ?? ItemPackId;

                bool isStandalone = GroupId == ItemId;

                if (isStandalone)
                {
                    cart.Items.RemoveAll(i => 
                        i.ItemId == ItemId &&
                        string.IsNullOrEmpty(i.GroupingId) && 
                        i.PackId == targetPackId);
                }
                else
                {
                    cart.Items.RemoveAll(i => 
                        i.GroupingId == GroupId && 
                        i.PackId == targetPackId);
                }
            }
        }

        public List<string> GetPacks(OrderCart cart)
        {
            return cart.Items.Select(i => i.PackId).Distinct().OrderBy(id => id).ToList();
        }

        public List<CartItem> GetItemsByPack(OrderCart cart, string packId)
        {
            return cart.Items.Where(i => i.PackId == packId).ToList();
        }

        public void RemovePack(OrderCart cart, string packId)
        {
            cart.Items.RemoveAll(i => i.PackId == packId);
        }

        public string GetNextPackId(OrderCart cart)
        {
            var packs = GetPacks(cart);
            if (!packs.Any()) return "pack1";
            
            var lastPackNumber = packs.Select(p => int.Parse(p.Replace("pack", ""))).Max();
            return $"pack{lastPackNumber + 1}";
        }

        public List<(string PackId, string PackName, int ItemCount)> GetPackListForEditing(OrderCart cart)
        {
            var packs = GetPacks(cart);
            return packs.Select(packId => 
            {
                var itemsInPack = GetItemsByPack(cart, packId);
                return (packId, $"Pack {packId.Replace("pack", "")}", itemsInPack.Count);
            }).ToList();
        }

    }
}
