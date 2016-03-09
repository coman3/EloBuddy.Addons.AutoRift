using EloBuddy;

namespace AutoRift
{
    internal class HeroInfo
    {
        public AIHeroClient Hero;
        public int Kills { get; private set; }

        public HeroInfo(AIHeroClient h)
        {
            Hero = h;
            Game.OnNotify += Game_OnNotify;
        }

        private void Game_OnNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnChampionKill && args.NetworkId == Hero.NetworkId)
                Kills++;
        }
    }
}
