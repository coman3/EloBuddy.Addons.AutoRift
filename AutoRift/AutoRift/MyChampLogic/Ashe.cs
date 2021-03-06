﻿using System.Linq;
using AutoRift.MainLogics;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace AutoRift.MyChampLogic
{
    internal class Ashe : IChampLogic
    {
        public float MaxDistanceForAa { get { return int.MaxValue; } }
        public float OptimalMaxComboDistance { get { return AutoWalker.P.AttackRange; } }
        public float HarassDistance { get { return AutoWalker.P.AttackRange; } }


        public Spell.Active Q;
        public Spell.Skillshot W, E, R;

        public Ashe()
        {
            SkillSequence = new[] {2, 1, 3, 2, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3};
            ShopSequence =
                "3340:Buy,1036:Buy,2003:StartHpPot,1053:Buy,1042:Buy,1001:Buy,3006:Buy,1036:Buy,1038:Buy,3072:Buy,2003:StopHpPot,1042:Buy,1051:Buy,3086:Buy,1042:Buy,1042:Buy,1043:Buy,3085:Buy,2015:Buy,3086:Buy,3094:Buy,1018:Buy,1038:Buy,3031:Buy,1037:Buy,3035:Buy,3033:Buy";
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1200, SkillShotType.Cone);
            E = new Spell.Skillshot(SpellSlot.E, 2500, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 3000, SkillShotType.Linear, 250, 1600, 130)
            {
                MinimumHitChance = HitChance.Medium,
                AllowedCollisionCount = 99
            };
            Game.OnTick += Game_OnTick;
        }

        public int[] SkillSequence { get; private set; }
        public LogicSelector Logic { get; set; }

        public string ShopSequence { get; private set; }

        public void Harass(AIHeroClient target)
        {
        }

        public void Survi()
        {
            if (R.IsReady() || W.IsReady())
            {
                AIHeroClient chaser =
                    EntityManager.Heroes.Enemies.FirstOrDefault(
                        chase => chase.Distance(AutoWalker.P) < 600 && chase.IsVisible());
                if (chaser != null)
                {
                    if (R.IsReady() && AutoWalker.P.HealthPercent() > 18)
                        R.Cast(chaser);
                    if (W.IsReady())
                        W.Cast(chaser);
                }
            }
        }

        public void Combo(AIHeroClient target)
        {
            if (R.IsReady() && target.HealthPercent() < 25 && AutoWalker.P.Distance(target) > 600 &&
                AutoWalker.P.Distance(target) < 1600 && target.IsVisible())
                R.Cast(target);
        }

        private void Game_OnTick(System.EventArgs args)
        {
            if (!R.IsReady()) return;
            AIHeroClient vic =
                EntityManager.Heroes.Enemies.FirstOrDefault(
                    v => v.IsVisible() &&
                         v.Health < AutoWalker.P.GetSpellDamage(v, SpellSlot.R) && v.Distance(AutoWalker.P) > 700 &&
                         AutoWalker.P.Distance(v) < 2500);
            if (vic == null) return;
            R.Cast(vic);
        }
    }
}