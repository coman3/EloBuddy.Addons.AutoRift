using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace AutoRift.Utilities.AutoShop
{
    internal enum ShopActionType
    {
        Buy = 1,
        Sell = 2,
        StartHpPot = 3,
        StopHpPot = 4
    }

    public static class ShopGlobals
    {
        public static int GoldForNextItem=999999;
        public static string Next;
    }

    internal class EasyShopV2
    {
        private readonly List<BuildElement> _buildElements;
        private readonly CheckBox _enabled;
        private bool _first = false;

        public EasyShopV2(List<BuildElement> elements, CheckBox en)
        {
            _enabled = en;
            _buildElements = elements;
            Shopping();
        }


        private void Shopping()
        {
            List<LoLItem> myit = BrutalItemInfo.MyItems();
            if (!_first&&(!_enabled.CurrentValue || !ObjectManager.Player.IsInShopRange() || !_buildElements.Any()))
            {
                _first = true;
                Core.DelayAction(Shopping, 300);
                return;
            }

            ShopGlobals.GoldForNextItem = 9999999;
            int currentPos = BrutalItemInfo.GetNum(_buildElements);
            if (currentPos == -1)
                ShopGlobals.Next = "Inventories mismatch, won't buy any items";
            if (currentPos == 0)
            {
                if (!myit.Any())
                {
                    Shop.BuyItem(_buildElements.First(el => el.Position == 1).Item.Id);
                    Core.DelayAction(Shopping, 800);
                    return;
                }
            }
            if (currentPos + 2 > _buildElements.Count)
            {
                Core.DelayAction(Shopping, RandGen.R.Next(400, 800));
                return;
            }

            if (_buildElements.First(b => b.Position == currentPos + 2).Action != ShopActionType.Buy)
                foreach (
                    BuildElement buildElement in
                        _buildElements.Where(b => b.Position > currentPos + 1).OrderBy(b => b.Position).ToList())
                {
                    if (buildElement.Action == ShopActionType.Buy || buildElement.Action == ShopActionType.Sell) break;

                    currentPos++;
                    if (currentPos + 2 > _buildElements.Count)
                    {
                        Core.DelayAction(Shopping, RandGen.R.Next(400, 800));
                        return;
                    }
                }
            

            if (currentPos < _buildElements.Count - 1)
            {
                BuildElement b = _buildElements.First(el => el.Position == currentPos + 2);
                if (b.Action == ShopActionType.Sell)
                {
                    int slot = BrutalItemInfo.GetItemSlot(_buildElements.First(el => el.Position == currentPos + 2).Item.Id);
                    if (slot != -1)
                    {
                        Shop.SellItem(slot);
                        
                    }
                    else
                    {
                        b = _buildElements.First(el => el.Position == currentPos + 3);
                    }
                }

                if (b.Action == ShopActionType.Buy)
                {
                    ShopGlobals.Next = b.Item.Name;
                    ShopGlobals.GoldForNextItem = BrutalItemInfo.BuyItemSim(myit, b.Item);
                    Shop.BuyItem(b.Item.Id);
                }

            }


            Core.DelayAction(() =>
            {
                if (currentPos == -1) return;
                List<BuildElement> cur = _buildElements.Where(b => b.Position < currentPos+2).ToList();

                int hp = cur.Count(e => e.Action == ShopActionType.StartHpPot) -
                         cur.Count(e => e.Action == ShopActionType.StopHpPot);
                if (hp > 0 && !AutoWalker.P.InventoryItems.Any(it => it.Id.IsHealthlyConsumable()))
                    Shop.BuyItem(ItemId.Health_Potion);
                else if (hp <= 0)
                {
                    int slot = BrutalItemInfo.GetHealtlyConsumableSlot();
                    if (slot != -1)
                       Shop.SellItem(slot);
                }
            }
                , 150);

            Core.DelayAction(Shopping, RandGen.R.Next(600, 1000));
        }
    }
}