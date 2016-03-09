using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AutoRift
{
    internal class LocalAwareness
    {
        private readonly List<HeroInfo> _heroTable;
        public readonly HeroInfo Me;
        public LocalAwareness()
        {
            _heroTable=new List<HeroInfo>();
            foreach (AIHeroClient h in EntityManager.Heroes.AllHeroes)
            {
                if (h.IsMe)
                {
                    Me = new HeroInfo(h);
                    _heroTable.Add(Me);
                }
                else
                    _heroTable.Add(new HeroInfo(h));
            }

        }


        public float LocalDomination(Vector3 pos)
        {
            float danger = 0;
            foreach (HeroInfo h in _heroTable.Where(h => h.Hero.Distance(pos) < 900 && h.Hero.IsVisible()))
            {
                if (h.Hero.Health <= 0) continue;
                danger += (-0.0042857142857143f * (h.Hero.Distance(pos) + 100) + 4.4285714285714f) *HeroStrength(h) * (h.Hero.IsAlly ? -1 : 1);
            }
            foreach (Obj_AI_Minion tt in ObjectManager.Get<Obj_AI_Minion>().Where(min=>min.Health>0&&min.Distance(pos)<600&&min.Name.StartsWith("H28-G")))
            {
                danger += 10000*(tt.IsEnemy?1:-1);
            }
            if (AutoWalker.P.GetNearestTurret().Distance(pos) < 1000) danger += 35000;
            if (AutoWalker.P.GetNearestTurret(false).Distance(pos) < 400) danger -= 35000;
            return danger;
        }

        public float HeroStrength(HeroInfo h)
        {
            return (h.Hero.HealthPercent)*(100 + h.Hero.Level*10 + h.Kills*5);
        }

        public float MyStrength()
        {
            return HeroStrength(Me);
        }
        public float HeroStrength(AIHeroClient h)
        {
            return HeroStrength(_heroTable.First(he => he.Hero == h));
        }

        public float LocalDomination(Obj_AI_Base ob)
        {
            return LocalDomination(ob.Position);
        }

    }
}
