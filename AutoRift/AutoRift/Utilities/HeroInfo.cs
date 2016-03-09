using EloBuddy;

namespace AutoRift.Utilities
{
    internal class HeroInfo
    {
        public AIHeroClient Hero;
        public int Kills { get; private set; }
        public int Kills2 { get { return Hero.ChampionsKilled; } }
        public int Deaths { get { return Hero.Deaths; } }
        public int Assists { get { return Hero.Assists; } }
        public int Farm { get { return Hero.MinionsKilled + Hero.NeutralMinionsKilled; } }
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