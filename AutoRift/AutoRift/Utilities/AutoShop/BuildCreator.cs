using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace AutoRift.Utilities.AutoShop
{
    internal class BuildCreator
    {
        private readonly string _buildFile;
        private readonly CheckBox _enabled;
        private readonly Label _l;
        private readonly Menu _menu;

        private readonly List<BuildElement> _myBuild;
        private readonly PropertyInfo _property;
        private readonly EasyShopV2 _shop;
        private readonly string _sugBuild;

        private readonly CheckBox _toDefault;

        public BuildCreator(Menu parentMenu, string dir, string build="")
        {
            _sugBuild = build;
            _property = typeof(CheckBox).GetProperty("Position");
            _buildFile = Path.Combine(dir + "\\" + AutoWalker.P.ChampionName + "-" + Game.MapId + ".txt");
            _l = new Label("Shopping list for " + Game.MapId);
            _enabled = new CheckBox("Auto buy enabled", true);
            _myBuild = new List<BuildElement>();

            _menu = parentMenu.AddSubMenu("AutoShop: " + AutoWalker.P.ChampionName, "AB_SHOP_" + AutoWalker.P.ChampionName);
            _menu.Add("eeewgrververv", _l);
            _menu.Add(AutoWalker.P.ChampionName + "enabled", _enabled);
            LoadBuild();
            _shop = new EasyShopV2(_myBuild, _enabled);







            Menu info = parentMenu.AddSubMenu("Shop-instructions");
            _toDefault=new CheckBox("Delete custom build and set default ADC build", false);

            PropertyInfo property2 = typeof(CheckBox).GetProperty("Size");

            property2.GetSetMethod(true).Invoke(_toDefault, new object[] { new Vector2(400, 25) });
            info.Add("defbuild", _toDefault);
            info.AddSeparator(150);
            info.AddLabel(
                @"
Commands(type them in the chat):

/b itemName  :buy an item, you don't need to type exact name for the item, just few first
characters, for example for BT its enough: /b thebloodt

/s itemName : sell an item

/buyhp:keep buying 1 hp potion(if not in champ's inventory already)
/stophp : stop buying hp potion and sell if any is owned.

AUTOSHOP WILL STOP WORKING IF FINDS ANY ITEMS IN INVENTORY THAT AREN'T
IN THE SEQUENCE.

Don't add to the list items that you can't buy, for example jungle items without smite.

Autoshop will stop if you have items that are not listed, so it's recommended
to sell whole inventory after changing list.

Builds are saved in C:\Users\Username\AppData\Roaming\AutoRift\Builds
you can copy/share them.

            ");






            _toDefault.OnValueChange += toDefault_OnValueChange;
            Chat.OnInput += Chat_OnInput;
            Drawing.OnEndScene += Drawing_OnEndScene;

        }

        private void toDefault_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;

            
            Core.DelayAction(() => { _toDefault.CurrentValue = false; }, 200);
            Reset();
            LoadBuild();
        }

        private void Reset()
        {
            if(File.Exists(_buildFile))
                File.Delete(_buildFile);
            foreach (BuildElement buildElement in _myBuild)
            {
                buildElement.Remove(_menu);
            }
            _myBuild.Clear();
        }

        private void AddElement(LoLItem it, ShopActionType ty)
        {
            if (ty != ShopActionType.Buy || ty != ShopActionType.Sell)
            {
                int hp = _myBuild.Count(e => e.Action == ShopActionType.StartHpPot) -
                         _myBuild.Count(e => e.Action == ShopActionType.StopHpPot);
                if (ty == ShopActionType.StartHpPot && hp != 0) return;
                if (ty == ShopActionType.StopHpPot && hp == 0) return;
            }

            BuildElement b = new BuildElement(this, _menu, it, _myBuild.Any() ? _myBuild.Max(a => a.Position) + 1 : 1, ty);

            List<LoLItem> c = new List<LoLItem>();
            BrutalItemInfo.InventorySimulator(_myBuild, c);
            b.Cost = BrutalItemInfo.InventorySimulator(new List<BuildElement> { b }, c);
            b.FreeSlots = 7 - c.Count;
            b.UpdateText();
            if (b.FreeSlots == -1)
            {
                Chat.Print("Couldn't add " + it + ", inventory is full.");
                b.Remove(_menu);
            }
            else
                _myBuild.Add(b);
        }

        private void LoadBuild()
        {
            if (!File.Exists(_buildFile))
            {
                if (!_sugBuild.Equals(string.Empty))
                {
                    LoadInternalBuild();
                }
                return;
            }
            try
            {
                string s = File.ReadAllText(_buildFile);
                if (s.Equals(string.Empty))
                {
                    Chat.Print("AutoRift: the build is empty.");
                    LoadInternalBuild();
                    return;
                }
                foreach (ItemAction ac in DeserializeBuild(s))
                {
                    AddElement(BrutalItemInfo.GetItemById(ac.Item), ac.T);
                }
            }
            catch (Exception e)
            {
                Chat.Print("AutoRift: couldn't load the build.");
                LoadInternalBuild();
                Console.WriteLine(e.Message);
            }
        }

        private void LoadInternalBuild()
        {
            try
            {
                if (_sugBuild.Equals(string.Empty))
                {
                    Chat.Print("AutoRift: internal build is empty.");
                    return;
                }
                foreach (ItemAction ac in DeserializeBuild(_sugBuild))
                {
                    AddElement(BrutalItemInfo.GetItemById(ac.Item), ac.T);
                }
            }
            catch (Exception e)
            {
                Chat.Print("AutoRift: internal build load failed.");
                Console.WriteLine(e.Message);
            }
            Chat.Print("AutoRift: loaded internal build(change it if you want!).");
        }

        private void SaveBuild()
        {
            File.WriteAllText(_buildFile, SerializeBuild());
        }

        private string SerializeBuild()
        {
            string s = string.Empty;
            foreach (BuildElement el in _myBuild.OrderBy(el => el.Position))
            {
                s += el.Item.Id + ":" + el.Action + ",";
            }
            return s.Equals(string.Empty) ? s : s.Substring(0, s.Length - 1);
        }

        private IEnumerable<ItemAction> DeserializeBuild(string serialized)
        {
            List<ItemAction> b = new List<ItemAction>();
            foreach (string s in serialized.Split(','))
            {
                ItemAction ac = new ItemAction { Item = -1 };
                foreach (string s2 in s.Split(':'))
                {
                    if (ac.Item == -1)
                        ac.Item = int.Parse(s2);
                    else
                        ac.T = (ShopActionType)Enum.Parse(typeof(ShopActionType), s2, true);
                }
                b.Add(ac);
            }
            return b;
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (!MainMenu.IsVisible) return;
            _property.GetSetMethod(true).Invoke(_enabled, new object[] { _l.Position + new Vector2(433, 0) });
            foreach (BuildElement ele in _myBuild)
            {
                ele.UpdatePos(new Vector2(_l.Position.X, _l.Position.Y + 10));
            }
        }

        public void MoveUp(int index)
        {
            if (index <= 2) return;
            BuildElement th = _myBuild.First(ele => ele.Position == index);
            BuildElement up = _myBuild.First(ele => ele.Position == index - 1);
            th.Position--;
            up.Position++;

            foreach (BuildElement el in _myBuild.OrderBy(b => b.Position))
            {
                List<LoLItem> c = new List<LoLItem>();
                BrutalItemInfo.InventorySimulator(_myBuild, c, el.Position - 1);
                el.Cost = BrutalItemInfo.InventorySimulator(new List<BuildElement> { el }, c);
                el.FreeSlots = 7 - c.Count;
                el.UpdateText();
            }
            SaveBuild();
        }

        public void MoveDown(int index)
        {
            if (index == _myBuild.Count || index == 2) return;
            BuildElement th = _myBuild.First(ele => ele.Position == index);
            BuildElement dn = _myBuild.First(ele => ele.Position == index + 1);
            th.Position++;
            dn.Position--;

            SaveBuild();
        }

        public bool Remove(int index)
        {
            if (_myBuild.Count > 1 && index == 1) return false;
            BuildElement th = _myBuild.First(ele => ele.Position == index);
            _myBuild.Remove(th);
            th.Remove(_menu);
            foreach (BuildElement el in _myBuild.OrderBy(b => b.Position).Where(b => b.Position > index))
            {
                el.Position--;


                List<LoLItem> c = new List<LoLItem>();
                BrutalItemInfo.InventorySimulator(_myBuild, c, el.Position - 1);
                el.Cost = BrutalItemInfo.InventorySimulator(new List<BuildElement> { el }, c);
                el.FreeSlots = 7 - c.Count;
                el.UpdateText();
            }


            SaveBuild();
            return true;
        }

        private void Chat_OnInput(ChatInputEventArgs args)
        {
            if (args.Input.ToLower().StartsWith("/b "))
            {
                args.Process = false;
                string itemName = args.Input.Substring(2);
                LoLItem i = BrutalItemInfo.FindBestItem(itemName);
                Chat.Print("Buy " + i.Name);

                if (_myBuild.Count == 0 && !i.Groups.Equals("RelicBase"))
                {
                    AddElement(BrutalItemInfo.GetItemById(3340), ShopActionType.Buy);
                    Chat.Print("Added also warding totem.");
                }
                AddElement(i, ShopActionType.Buy);
                SaveBuild();
            }
            else if (args.Input.ToLower().StartsWith("/s "))
            {
                args.Process = false;
                string itemName = args.Input.Substring(2);
                LoLItem i = BrutalItemInfo.FindBestItemAll(itemName);
                Chat.Print("Sell " + i.Name);

                AddElement(i, ShopActionType.Sell);
                SaveBuild();
            }
            else if (args.Input.ToLower().Equals("/buyhp"))
            {
                if (_myBuild.Count == 0)
                {
                    AddElement(BrutalItemInfo.GetItemById(3340), ShopActionType.Buy);
                    Chat.Print("Added also warding totem.");
                }
                AddElement(BrutalItemInfo.GetItemById(2003), ShopActionType.StartHpPot);
                SaveBuild();
                args.Process = false;
            }
            else if (args.Input.ToLower().Equals("/stophp"))
            {
                if (_myBuild.Count == 0)
                {
                    AddElement(BrutalItemInfo.GetItemById(3340), ShopActionType.Buy);
                    Chat.Print("Added also warding totem.");
                }
                AddElement(BrutalItemInfo.GetItemById(2003), ShopActionType.StopHpPot);
                SaveBuild();
                args.Process = false;
            }
        }

        private struct ItemAction
        {
            public ShopActionType T;
            public int Item;
        }
    }
}