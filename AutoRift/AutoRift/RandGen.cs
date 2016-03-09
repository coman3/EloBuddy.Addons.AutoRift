using System;
using EloBuddy;
using EloBuddy.SDK;

namespace AutoRift
{
    internal static class RandGen
    {
        private static int _lastPath=1;
        public static Random R { get; private set; }

        static RandGen()
        {
            R=new Random();
            Core.DelayAction(ChangeSeed, 5000);
            Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
        }

        private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            _lastPath = DateTime.Now.Millisecond;
        }

        private static void ChangeSeed()
        {
            R = new Random(DateTime.Now.Millisecond * _lastPath * (int)(Game.CursorPos.X+1000) * (int)(Game.CursorPos.Y+1000));
            Core.DelayAction(ChangeSeed, R.Next(10000, 20000));
        }

        public static void Start()
        {
        }
    }
}
