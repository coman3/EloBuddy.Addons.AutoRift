using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AutoRift.Utilities
{
    internal class LocalAwareness
    {
        public readonly List<HeroInfo> HeroTable;
        public readonly HeroInfo Me;

        public LocalAwareness()
        {
            HeroTable = new List<HeroInfo>();
            foreach (AIHeroClient h in EntityManager.Heroes.AllHeroes)
            {
                if (h.IsMe)
                {
                    Me = new HeroInfo(h);
                    HeroTable.Add(Me);
                }
                else
                    HeroTable.Add(new HeroInfo(h));
            }
        }


        public float LocalDomination(Vector3 pos)
        {
            float danger = 0;
            foreach (
                HeroInfo h in
                    HeroTable.Where(
                        hh => hh.Hero.IsVisible() && !hh.Hero.IsDead() && hh.Hero.Position.Distance(pos) < 900))
            {
                if (h.Hero.IsZombie)
                {
                    danger += (-0.0042857142857143f * (h.Hero.Distance(pos) + 100) + 4.4285714285714f) * 15000 *
(h.Hero.IsEnemy ? 1 : -1);
                }

                else
                {
                    danger += (-0.0042857142857143f * (h.Hero.Distance(pos) + 100) + 4.4285714285714f) * HeroStrength(h) *
          (h.Hero.IsEnemy ? 1 : -1);
                }

            }
            foreach (
                Obj_AI_Minion tt in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(min => min.Health > 0 && min.Distance(pos) < 550+AutoWalker.P.BoundingRadius))
            {
                if(tt.Name.StartsWith("H28-G"))
                    danger += 10000*(tt.IsAlly ? -1 : 1);
                else if (tt.CombatType==GameObjectCombatType.Ranged)
                    danger += 800 * (tt.IsAlly ? -1 : 2);
                else if (tt.CombatType == GameObjectCombatType.Ranged && tt.Distance(AutoWalker.P) < 130 + AutoWalker.P.BoundingRadius)
                    danger += 800 * (tt.IsAlly ? -1 : 2);
            }
            if (AutoWalker.P.GetNearestTurret().Distance(pos) < 1000 + AutoWalker.P.BoundingRadius) danger += 35000;
            if (AutoWalker.P.GetNearestTurret(false).Distance(pos) < 400) danger -= 35000;
            return danger;
        }

        public float HeroStrength(HeroInfo h)
        {
            return h.Hero.HealthPercent()*(100 + h.Hero.Level*10 + h.Kills*5);
        }

        public float MyStrength()
        {
            return HeroStrength(Me);
        }

        public float HeroStrength(AIHeroClient h)
        {
            return HeroStrength(HeroTable.First(he => he.Hero == h));
        }

        public float LocalDomination(Obj_AI_Base ob)
        {
            return LocalDomination(ob.Position);
        }
    }
}