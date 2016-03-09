using EloBuddy;

namespace AutoRift.Utilities
{
    internal enum Lane
    {
        Unknown,
        Top,
        Mid,
        Bot,
        Jungle,
        Hq,
        Spawn
    }

    internal class ChampLane
    {
        public readonly AIHeroClient Champ;
        public readonly Lane Lane;

        public ChampLane(AIHeroClient champ, Lane lane)
        {
            this.Champ = champ;
            this.Lane = lane;
        }
    }
}