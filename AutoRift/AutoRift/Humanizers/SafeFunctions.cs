using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace AutoRift.Humanizers
{
    internal static class SafeFunctions
    {
        private static float _lastPing;
        private static float _lastChat;

        static SafeFunctions()
        {
            _lastChat = 0;
        }


        public static void Ping(PingCategory cat, Vector3 pos)
        {
            if (MainMenu.GetMenu("AB").Get<CheckBox>("disablepings").CurrentValue) return;
            if (_lastPing > Game.Time) return;
            _lastPing = Game.Time + 1.8f;
            Core.DelayAction(() => TacticalMap.SendPing(cat, pos), RandGen.R.Next(450, 800));
        }

        public static void Ping(PingCategory cat, GameObject target)
        {
            if (MainMenu.GetMenu("AB").Get<CheckBox>("disablepings").CurrentValue) return;
            if (_lastPing > Game.Time) return;
            _lastPing = Game.Time + 1.8f;
            Core.DelayAction(() => TacticalMap.SendPing(cat, target), RandGen.R.Next(450, 800));
        }

        public static void SayChat(string msg)
        {
            if (MainMenu.GetMenu("AB").Get<CheckBox>("disablechat").CurrentValue) return;
            if (_lastChat > Game.Time) return;
            _lastChat = Game.Time + .8f;
            Core.DelayAction(() => Chat.Say(msg), RandGen.R.Next(150, 400));
        }
    }
}