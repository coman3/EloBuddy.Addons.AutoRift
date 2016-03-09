using EloBuddy;

namespace AutoRift
{
    enum Lane
    {
        Unknown,
        Top,
        Mid,
        Bot,
        Hq,
        Spawn
    }
    class ChampLane
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
