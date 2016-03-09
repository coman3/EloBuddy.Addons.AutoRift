using System;
using System.Collections.Generic;
using System.Linq;
using AutoRift.Properties;
using EloBuddy;
using Newtonsoft.Json.Linq;

namespace AutoRift.Utilities.AutoShop
{
    internal static class BrutalItemInfo
    {
        public static readonly LoLItem[] ItemDb;
        public static readonly LoLItem[] AvItemDb;

        static BrutalItemInfo()
        {
            List<LoLItem> all = new List<LoLItem>();
            List<LoLItem> av = new List<LoLItem>();
            foreach (LoLItem loLItem in ParseItems())
            {
                all.Add(loLItem);
                if (loLItem.Purchasable && loLItem.Maps.Contains((int)Game.MapId) &&
                    (loLItem.RequiredChampion == String.Empty || loLItem.RequiredChampion == AutoWalker.P.ChampionName))
                    av.Add(loLItem);
            }
            ItemDb = all.ToArray();
            AvItemDb = av.ToArray();
        }

        public static LoLItem FindBestItem(this List<LoLItem> items, string name)
        {
            return items.OrderByDescending(it => it.Name.Match(name)).First();
        }

        public static LoLItem FindBestItem(this LoLItem[] items, string name)
        {
            return items.OrderByDescending(it => it.Name.Match(name)).First();
        }

        public static LoLItem FindBestItem(string name)
        {
            return AvItemDb.OrderByDescending(it => it.Name.Match(name)).First();
        }

        public static LoLItem FindBestItemAll(string name)
        {
            return ItemDb.OrderByDescending(it => it.Name.Match(name)).First();
        }

        public static LoLItem FindItemByID(this LoLItem[] items, int id)
        {
            return items.OrderByDescending(it => it.Id == id).First();
        }

        public static LoLItem FindItemByID(this List<LoLItem> items, int id)
        {
            return items.OrderByDescending(it => it.Id == id).First();
        }

        public static List<LoLItem> MyItems()
        {
            List<LoLItem> l = AutoWalker.P.InventoryItems.Select(s => ItemDb.FindItemByID((int)s.Id)).ToList();
            l.Remove(l.FirstOrDefault(le => le.Id == 1411)); //TODO !! Remove this when eb or rito will fix it
            return l;
        }

        public static LoLItem GetItemById(int id)
        {
            return ItemDb.First(it => it.Id == id);
        }


        public static List<LoLItem> ParseItems()
        {
            JObject data = JObject.Parse(Resources.item);
            List<LoLItem> loLItems = new List<LoLItem>();
            foreach (JToken token in data.GetValue("data"))
            {
                JToken t = token.First;

                List<int> maps = new List<int>();
                if ((bool)t["maps"]["1"]) maps.Add(1);
                if ((bool)t["maps"]["8"]) maps.Add(8);
                if ((bool)t["maps"]["10"]) maps.Add(10);
                if ((bool)t["maps"]["11"]) maps.Add(11);
                if ((bool)t["maps"]["12"]) maps.Add(12);
                if ((bool)t["maps"]["14"]) maps.Add(14);

                List<int> fromItems = new List<int>();
                if (t["from"] != null)
                    fromItems.AddRange(t["from"].Select(tok => (int)tok));

                List<int> toItems = new List<int>();
                if (t["to"] != null)
                    toItems.AddRange(t["to"].Select(tok => (int)tok));

                List<string> tags = new List<string>();
                if (t["tags"] != null)
                    tags.AddRange(t["tags"].Select(tok => tok.ToString()));

                loLItems.Add(new LoLItem(t["name"].ToString(), t["description"].ToString(),
                    t["sanitizedDescription"].ToString(),
                    t["plaintext"] == null ? String.Empty : t["plaintext"].ToString(),
                    (int)t["id"], (int)t["gold"]["base"], (int)t["gold"]["total"], (int)t["gold"]["sell"],
                    (bool)t["gold"]["purchasable"],
                    t["requiredChampion"] == null ? String.Empty : t["requiredChampion"].ToString(), maps.ToArray(),
                    fromItems.ToArray(), toItems.ToArray(), t["depth"] == null ? -1 : (int)t["depth"], tags.ToArray(),
                    t["cq"] == null ? String.Empty : t["cq"].ToString(),
                    t["group"] == null ? String.Empty : t["group"].ToString()));
            }
            return loLItems;
        }

        public static int InventorySimulator(List<BuildElement> elements, List<LoLItem> playerInv,
            int num = Int32.MaxValue)
        {
            int n = 0;
            int gold = 0;
            foreach (BuildElement el in elements.OrderBy(el => el.Position))
            {
                if (n >= num)
                    return gold;
                n++;
                if (el.Action == ShopActionType.Buy)
                {
                    gold += BuyItemSim(playerInv, el.Item);
                    playerInv.Add(el.Item);
                }
                else if (el.Action == ShopActionType.Sell)
                {
                    gold -= SellItemSim(playerInv, el.Item);
                }
                else if (el.Action == ShopActionType.StartHpPot)
                {
                    gold += BuyItemSim(playerInv, el.Item);
                    if (playerInv.FirstOrDefault(ii => ii.Id == (int)ItemId.Health_Potion) == null)
                        playerInv.Add(el.Item);
                }
                else if (el.Action == ShopActionType.StopHpPot)
                {
                    playerInv.Remove(el.Item);
                }
            }
            return gold;
        }

        public static int GetNum(List<BuildElement> elements)
        {
            int n = 0;
            List<LoLItem> myItems = MyItems();
            List<LoLItem> virtInv = new List<LoLItem>();
            foreach (BuildElement el in elements.OrderBy(el => el.Position))
            {
                if (el.Action == ShopActionType.Buy)
                {
                    BuyItemSim(virtInv, el.Item);
                    virtInv.Add(el.Item);
                    if (virtInv.Equal(myItems)) return n;
                }

                n++;
            }
            return -1;
        }

        /*public static bool Equal(this List<LoLItem> virtInv, List<LoLItem> charInv, bool ignorePotions = true)
        {
            if (!virtInv.Any() && !charInv.Any()) return true;
            List<LoLItem> lTwo = new List<LoLItem>(charInv);
            foreach (LoLItem itOne in virtInv)
            {
                bool cont = false;
                if (itOne.IsHealthlyConsumable()) continue;
                foreach (LoLItem itTwo in lTwo)
                {
                    Chat.Print(itTwo.IsHealthlyConsumable());
                    if (itOne.id == itTwo.id || itTwo.IsHealthlyConsumable() || !itTwo.purchasable)
                    {
                        lTwo.Remove(itTwo);
                        cont = true;
                        break;
                    }
                }
                if (!cont) return false;
            }

            return !lTwo.Any();
        }*/

        private static bool Equal(this List<LoLItem> virtInv, List<LoLItem> charInv, bool ignorePotions = true)
        {
            if (!virtInv.Any() && !charInv.Any())
                return true;
            List<LoLItem> inv1 = virtInv.Where(it => !it.IsHealthlyConsumable()&&it.Id!=3599).ToList();
            List<LoLItem> inv2 = charInv.Where(it => !it.IsHealthlyConsumable() && it.Id != 3599).ToList();

            foreach (LoLItem item1 in inv1)
            {
                
                foreach (LoLItem item2 in inv2.ToList())
                {
                    if (SameItems(item1.Id, item2.Id))
                    {
                        inv2.Remove(item2);
                        break;
                    }
                }
            }

            if (inv2.Any())
                //Chat.Print("1: " + inv1.OrderBy(it => it.name).Concatenate(", "));
            //Chat.Print("2: " + inv2.OrderBy(it => it.name).Concatenate(", "));
            {

            }
            return !inv2.Any();
        }

        public static bool SameItems(int id1, int id2)
        {
            if (id1 == id2) return true;
            if ((id1 == 3003 && id2 == 3040) || (id2 == 3003 && id1 == 3040)) return true;//seraphs
            if ((id1 == 3004 && id2 == 3043) || (id2 == 3004 && id1 == 3043)) return true;//muramana

            return false;
        }

        public static int BuyItemSim(List<LoLItem> inventory, LoLItem item, bool root = true)
        {
            if (!root && inventory.Any(it => it.Id == item.Id))
            {
                inventory.Remove(inventory.First(it => it.Id == item.Id));
                return 0;
            }
            if (item.FromItems.Length == 0)
            {
                return item.BaseGold;
            }
            int gold = item.BaseGold +
                       item.FromItems.Sum(
                           fromItemId => BuyItemSim(inventory, ItemDb.First(it => it.Id == fromItemId), false));
            return gold;
        }

        public static int SellItemSim(List<LoLItem> inventory, LoLItem item)
        {
            if (inventory.Contains(item))
            {
                inventory.Remove(item);
                return -item.SellGold;
            }
            return -1;
        }

        public static int GetItemSlot(int id)
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; i++)
            {
                if ((int)ObjectManager.Player.InventoryItems[i].Id == id)
                    return i;
            }
            return -1;
        }

        public static int GetHealtlyConsumableSlot()
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; i++)
            {
                if (ObjectManager.Player.InventoryItems[i].Id.IsHealthlyConsumable())
                    return i;
            }
            return -1;
        }

        public static int GetHPotionSlot()
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; i++)
            {
                if (ObjectManager.Player.InventoryItems[i].Id.IsHPotion())
                    return i;
            }
            return -1;
        }
    }
}