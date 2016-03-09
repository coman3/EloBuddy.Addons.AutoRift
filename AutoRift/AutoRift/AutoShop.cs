using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AutoRift
{
    internal class AutoShop
    {
        private float _maxTime = 1.2f;
        private float _minTime = .4f;
        private readonly List<int> _itemsQueue;

        public float AdditionalMaxRandomTime
        {
            get { return _maxTime; }
            set
            {
                if (value >= 0 && value <= 4 && value > _minTime)
                    _maxTime = value;
                else
                    Console.WriteLine("Shop MaxTime not set, invalid value.");
            }
        }

        public float AdditionalMinRandomTime
        {
            get { return _minTime; }
            set
            {
                if (value >= 0 && value <= 4 && value < _maxTime)
                    _minTime = value;
                else
                    Console.WriteLine("Shop MinTime not set, invalid value.");
            }
        }
        private float _lastShopActionTime;

        public AutoShop()
        {
            _itemsQueue = new List<int>();
        }

        public void Buy(int id)
        {
            if (ObjectManager.Player.InventoryItems.Length == 7) return;
            _itemsQueue.Add(id);
            if (_itemsQueue.Count == 1)
                CheckQueue();
        }

        public void Buy(ItemId id)
        {
            Buy((int)id);
        }

        public void BuyIfNotOwned(ItemId id)
        {
            if (ObjectManager.Player.InventoryItems.All(it => it.Id != id))
                Buy(id);
        }
        public void BuyIfNotOwned(int id)
        {
            BuyIfNotOwned((ItemId)id);
        }

        private void CheckQueue()
        {
            if (_lastShopActionTime > Game.Time)
            {
                Core.DelayAction(CheckQueue, (int)((_lastShopActionTime - Game.Time) * 1000));
                return;
            }

            int item = _itemsQueue.Last();
            _itemsQueue.Remove(item);
            if (Shop.BuyItem(item))
            {
                _lastShopActionTime = Game.Time + RandGen.R.NextFloat(_minTime, _maxTime);
                if (_itemsQueue.Any())
                    Core.DelayAction(CheckQueue, (int)((_lastShopActionTime - Game.Time) * 1000));
                return;
            }

            if (_itemsQueue.Any())
                CheckQueue();

        }

    }
}
