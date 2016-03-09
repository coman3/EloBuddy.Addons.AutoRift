using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AutoRift
{
    internal static class SafeFunctions
    {
        private static float _lastPing;
        private static float _lastChat;
        private static readonly AutoShop AutoShop;

        static SafeFunctions()
        {
            AutoShop=new AutoShop();
        }

        public static void BuyItem(int itemId)
        {
            AutoShop.Buy(itemId);
        }

        public static void BuyItem(ItemId itemId)
        {
            AutoShop.Buy(itemId);
        }

        public static void BuyIfNotOwned(int itemId)
        {
            AutoShop.BuyIfNotOwned(itemId);
        }

        public static void BuyIfNotOwned(ItemId itemId)
        {
            AutoShop.BuyIfNotOwned(itemId);
        }

        public static void Ping(PingCategory cat, Vector3 pos)
        {
            if (_lastPing > Game.Time) return;
            _lastPing = Game.Time + .8f;
            Core.DelayAction(()=>TacticalMap.SendPing(cat, pos), RandGen.R.Next(150, 400));
        }

        public static void Ping(PingCategory cat, GameObject target)
        {
            if (_lastPing > Game.Time) return;
            _lastPing = Game.Time + .8f;
            Core.DelayAction(() => TacticalMap.SendPing(cat, target), RandGen.R.Next(150, 400));
        }

        public static void SayChat(string msg)
        {
            if (_lastChat > Game.Time) return;
            _lastChat = Game.Time + .8f;
            Core.DelayAction(() => Chat.Say(msg), RandGen.R.Next(150, 400));
        }
        
    }
}
