using System.Reflection;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace AutoRift.Utilities.AutoShop
{
    internal class BuildElement
    {
        public readonly ShopActionType Action;
        private readonly BuildCreator _bc;
        public readonly LoLItem Item;
        private readonly PropertyInfo _property;
        public int Cost;
        private Label _costSlots;
        public int FreeSlots;
        private Label _itemName;
        public int P;
        private CheckBox _removeBox;
        private CheckBox _upBox;

        public BuildElement(BuildCreator bc, Menu menu, LoLItem item, int index, ShopActionType action)
        {
            this.Action = action;
            this._bc = bc;
            this.Item = item;
            P = index;

            _upBox = new CheckBox("up", false);
            _removeBox = new CheckBox("remove", false);
            _itemName = new Label(" ");
            _costSlots = new Label(" ");

            PropertyInfo property2 = typeof (CheckBox).GetProperty("Size");

            property2.GetSetMethod(true).Invoke(_itemName, new object[] {new Vector2(400, 0)});
            property2.GetSetMethod(true).Invoke(_costSlots, new object[] {new Vector2(400, 0)});
            property2.GetSetMethod(true).Invoke(_upBox, new object[] {new Vector2(40, 20)});
            property2.GetSetMethod(true).Invoke(_removeBox, new object[] {new Vector2(80, 20)});


            menu.Add(Position + "nam" + RandGen.R.Next(), _itemName);
            menu.Add(Position + "cs" + RandGen.R.Next(), _costSlots);
            menu.Add(Position + "up" + RandGen.R.Next(), _upBox);
            menu.Add(Position + "rem" + RandGen.R.Next(), _removeBox);
            UpdateText();

            _upBox.CurrentValue = false;
            _removeBox.CurrentValue = false;
            _upBox.OnValueChange += upBox_OnValueChange;
            _removeBox.OnValueChange += removeBox_OnValueChange;
            _property = typeof (CheckBox).GetProperty("Position");
        }

        public int Position
        {
            get { return P; }
            set
            {
                P = value;
                UpdateText();
            }
        }


        public void UpdateText()
        {
            if (Action == ShopActionType.Buy || Action == ShopActionType.Sell)
            {
                _itemName.CurrentValue = P.ToString().PadLeft(2, ' ') + ")" + Action.ToString().PadLeft(6, ' ') + "    " +
                                        Item.Name;
                _costSlots.CurrentValue = "Free slots: " + FreeSlots + "      Cost: " + Cost.ToString().PadLeft(4, ' ');
            }
            else
            {
                _itemName.CurrentValue = P.ToString().PadLeft(2, ' ') + ")   " + Action;
                _costSlots.CurrentValue = "Free slots: " + FreeSlots;
            }
        }

        private void removeBox_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;
            if (!_bc.Remove(P))
                Core.DelayAction(() => { _removeBox.CurrentValue = false; }, 1);
        }


        private void upBox_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;
            _bc.MoveUp(P);
            Core.DelayAction(() => { _upBox.CurrentValue = false; }, 1);
        }


        public void UpdatePos(Vector2 basePos)
        {
            _property.GetSetMethod(true).Invoke(_itemName, new object[] {basePos + new Vector2(0, P*20)});
            _property.GetSetMethod(true).Invoke(_costSlots, new object[] {basePos + new Vector2(250, P*20)});
            _property.GetSetMethod(true).Invoke(_upBox, new object[] {basePos + new Vector2(440, P*20 - 12)});
            _property.GetSetMethod(true).Invoke(_removeBox, new object[] {basePos + new Vector2(490, P*20 - 12)});
        }


        public void Remove(Menu menu)
        {
            menu.Remove(_itemName);
            menu.Remove(_costSlots);
            menu.Remove(_removeBox);
            menu.Remove(_upBox);
            _costSlots = null;
            _itemName = null;
            _removeBox = null;
            _upBox = null;
        }
    }
}