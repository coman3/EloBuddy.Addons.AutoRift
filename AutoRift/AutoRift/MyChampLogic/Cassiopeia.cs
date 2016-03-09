using System;
using System.IO;
using System.Linq;
using AutoRift.MainLogics;
using AutoRift.Utilities;
using AutoRift.Utilities.AutoShop;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.MyChampLogic
{
    internal class Cassiopeia : IChampLogic
    {
        public float MaxDistanceForAa { get { return 500; } }
        public float OptimalMaxComboDistance { get { return 500; } }
        public float HarassDistance { get { return 500; } }
        private readonly Spell.Skillshot _q, _w, _r;
        private readonly Spell.Targeted _e;
        private int _minManaHarass = 35;
        private int _tick;
        private bool _isTearOwned;
        private string _dmg;
        public Cassiopeia()
        {
            if (BrutalExtensions.GetGameType().Equals("bot_intermediate"))
            {
                AutoWalker.EndGame += End;
                Core.DelayAction(Fl, 10000);
            }
            ShopSequence =
                "3340:Buy,2003:StartHpPot,1056:Buy,1027:Buy,3070:Buy,1001:Buy,1058:Buy,3003:Buy,3020:Buy,1028:Buy,1011:Buy,1058:Buy,2003:StopHpPot,3116:Buy,1004:Buy,1004:Buy,3114:Buy,1052:Buy,3108:Buy,3165:Buy,1056:Sell,1058:Buy,3089:Buy,1028:Buy,3136:Buy,3151:Buy";
            _q = new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Circular, 600, int.MaxValue, 35);
            _w = new Spell.Skillshot(SpellSlot.W, 850, SkillShotType.Circular, 500, 2500, 90);
            _r = new Spell.Skillshot(SpellSlot.R, 500, SkillShotType.Cone, 650, int.MaxValue, 75);
            _e = new Spell.Targeted(SpellSlot.E, 700);
            UpdateTearStatus();
            Game.OnTick += Game_OnTick;
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
            
        }

        private void Fl()
        {
            Core.DelayAction(Fl, 10000);
        }

        private void End(object sender, EventArgs e)
        {

        }

        private void UpdateTearStatus()
        {
            _isTearOwned = BrutalItemInfo.GetItemSlot(3070) != -1 || BrutalItemInfo.GetItemSlot(3003) != -1;
            Core.DelayAction(UpdateTearStatus, 5000);
        }



        void Drawing_OnDraw(EventArgs args)
        {
            foreach (var vector3 in AutoWalker.P.Path)
            {
                Circle.Draw(new ColorBGRA(100, 100, 100, 255), 10, vector3);
            }
            Drawing.DrawText(900, 10, Color.Chocolate, _dmg, 70);
            /*AIHeroClient buf =
    EntityManager.Heroes.AllHeroes.Where(h => h.Distance(Game.CursorPos) < 800)
        .OrderBy(e => e.Distance(Game.CursorPos))
        .FirstOrDefault();
            if (buf != null)
            {
                int y = 0;
                foreach (BuffInstance buff in buf.Buffs)
                {
                    Drawing.DrawText(500, 500 + y, Color.Chocolate, "Name: " +buff.Name+"  DisplayName: " +buff.DisplayName, 10);
                    y += 20;
                }
            }
            

            AIHeroClient t=EntityManager.Heroes.Enemies.FirstOrDefault(en=>en.Distance(Game.CursorPos)<600);
            if (t != null)
            {
                Vector2 pos = Game.CursorPos.WorldToScreen();
                pos.Y -= 200;
                /*int offset = 0;
                foreach (BuffInstance buff in t.Buffs)
                {
                    Drawing.DrawText(pos.X, pos.Y+offset, Color.Aqua, buff.Name+" "+(buff.EndTime-Game.Time));
                    offset += 20;
                }
                
                float ti = TimeForAttack(t, 600);
                Drawing.DrawText(pos.X, pos.Y + 60, Color.Aqua, ti + " " + EstDmg(t, ti) + "  " + (t.Health - EstDmg(t, ti)));
            }*/
        }

        private void Game_OnTick(EventArgs args)
        {
            //Chat.Print(AutoWalker.Recalling());
            AIHeroClient t = EntityManager.Heroes.Enemies.Where(en => en.IsVisible() && en.Distance(Game.CursorPos) < 630).OrderBy(en => en.Health).FirstOrDefault();
            if (t != null)
            {
                float ti = TimeForAttack(t, 630);
                float dm = 0;
                if (EstDmg(t, ti) > 0)
                {
                    dm = EstDmg(t, ti);
                }
                if (AutoWalker.Ignite != null && AutoWalker.Ignite.IsReady() && t.Health > dm && t.Health < dm + (50 + 20 * AutoWalker.P.Level))
                    AutoWalker.UseIgnite(t);
                _dmg = dm + ", " + (t.Health - dm);
            }

            if (_isTearOwned && _q.IsReady() && AutoWalker.P.ManaPercent > 95 && !AutoWalker.Recalling() && !EntityManager.Heroes.Enemies.Any(en => en.Distance(AutoWalker.P) < 2000) && !EntityManager.MinionsAndMonsters.EnemyMinions.Any(min => min.Distance(AutoWalker.P) < 1000))
            {

                _q.Cast((Prediction.Position.PredictUnitPosition(AutoWalker.P, 2000) +
                       new Vector2(RandGen.R.NextFloat(-200, 200), RandGen.R.NextFloat(-200, 200))).To3D());
            }



            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LaneClear)
            {
                if (!EntityManager.Heroes.Enemies.Any(en => en.Distance(AutoWalker.P) < 650 + en.BoundingRadius))
                {
                    if (_q.IsReady() && AutoWalker.P.MaxMana > 750 && AutoWalker.P.ManaPercent > 65)
                    {
                        _tick++;
                        if (_tick % 5 != 0) return;
                        EntityManager.MinionsAndMonsters.FarmLocation f =
                            EntityManager.MinionsAndMonsters.GetCircularFarmLocation(EntityManager.MinionsAndMonsters.GetLaneMinions(radius: 850), 250, 700);
                        if (f.HitNumber >= 4 || (f.HitNumber == 3 && AutoWalker.P.ManaPercent > 80))
                        {
                            _q.Cast(f.CastPosition);
                        }


                    }

                    if (_e.IsReady())
                    {
                        Obj_AI_Minion minionToE = EntityManager.MinionsAndMonsters.GetLaneMinions(radius: 850).FirstOrDefault(min => min.HasBuffOfType(BuffType.Poison) && min.Distance(AutoWalker.P) < min.BoundingRadius + _e.Range && Prediction.Health.GetPrediction(min, 100) < AutoWalker.P.GetSpellDamage(min, SpellSlot.E) && Prediction.Health.GetPrediction(min, 100) > 0);
                        if (minionToE != null)
                            _e.Cast(minionToE);
                        else if (!EntityManager.Heroes.Enemies.Any(en => en.IsVisible() && en.Distance(AutoWalker.P) < 1200))
                        {
                            minionToE = EntityManager.MinionsAndMonsters.GetLaneMinions(radius: 850).FirstOrDefault(min => min.Distance(AutoWalker.P) < min.BoundingRadius + _e.Range && Prediction.Health.GetPrediction(min, 200) < AutoWalker.P.GetSpellDamage(min, SpellSlot.E) && Prediction.Health.GetPrediction(min, 200) > 0);
                            if (minionToE != null)
                                _e.Cast(minionToE);
                        }
                    }
                }


                if (AutoWalker.P.ManaPercent < 15) return;
                AIHeroClient poorVictim = TargetSelector.GetTarget(850, DamageType.Magical, addBoundingRadius: true);
                if (poorVictim != null && _minManaHarass < AutoWalker.P.HealthPercent)
                {
                    if (_q.IsReady())
                    {
                        PredictionResult pr = _q.GetPrediction(poorVictim);
                        if (pr.HitChancePercent > 35)
                        {
                            _q.Cast(pr.CastPosition);

                        }
                    }
                    if (_e.IsReady())
                    {
                        AIHeroClient candidateForE = EntityManager.Heroes.Enemies.Where(
                            en =>
                                en.HasBuffOfType(BuffType.Poison) && en.IsTargetable &&
                                !en.HasBuffOfType(BuffType.SpellImmunity) && !en.HasBuffOfType(BuffType.Invulnerability) &&
                                en.Distance(AutoWalker.P) < en.BoundingRadius + _e.Range && !en.IsDead())
                            .OrderBy(en => en.Health / AutoWalker.P.GetSpellDamage(en, SpellSlot.E))
                            .FirstOrDefault();
                        if (candidateForE != null)
                            _e.Cast(candidateForE);

                    }

                }
            }
            else if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Flee)
            {
                AIHeroClient poorVictim = TargetSelector.GetTarget(700, DamageType.Magical, addBoundingRadius: true) ??
                                          TargetSelector.GetTarget(850, DamageType.Magical, addBoundingRadius: true);

                if (poorVictim != null)
                {
                    if (_q.IsReady())
                    {
                        PredictionResult pr = _q.GetPrediction(poorVictim);
                        if (pr.HitChancePercent > 30)
                        {
                            _q.Cast(pr.CastPosition);
                        }

                    }
                    if (_e.IsReady() && (poorVictim.HasBuffOfType(BuffType.Poison) || AutoWalker.P.GetSpellDamage(poorVictim, SpellSlot.E) > poorVictim.Health))
                        _e.Cast(poorVictim);
                    else if (_e.IsReady())
                    {
                        AIHeroClient an = EntityManager.Heroes.Enemies.Where(en => en.HasBuffOfType(BuffType.Poison) && AutoWalker.P.Distance(en) < _e.Range + en.BoundingRadius).OrderBy(en => en.Health / AutoWalker.P.GetSpellDamage(en, SpellSlot.E))
                            .FirstOrDefault();
                        if (an != null)
                            _e.Cast(an);
                    }
                    if (!poorVictim.HasBuffOfType(BuffType.Poison) && _w.IsReady() || poorVictim.Distance(AutoWalker.P) > 650)
                    {
                        PredictionResult pr = _w.GetPrediction(poorVictim);
                        if (pr.HitChance >= HitChance.Medium)
                        {
                            _w.Cast(pr.CastPosition);
                        }
                    }
                    if (_r.IsReady() && poorVictim.HasBuffOfType(BuffType.Poison) && AutoWalker.P.ManaPercent > 35 && poorVictim.Distance(AutoWalker.P) > 200 && poorVictim.Distance(AutoWalker.P) < 600 + poorVictim.BoundingRadius && poorVictim.IsFacing(AutoWalker.P) && poorVictim.HealthPercent > 30 && poorVictim.HealthPercent < 60)
                        _r.Cast(Prediction.Position.PredictUnitPosition(poorVictim, 300).To3D());
                    if (_r.IsReady() && poorVictim.Distance(AutoWalker.P) < 600 && EntityManager.Heroes.Enemies.Count(en => en.IsVisible() && !en.IsDead() && en.Distance(AutoWalker.P) < 600) >= 2)
                        _r.Cast(Prediction.Position.PredictUnitPosition(poorVictim, 400).To3D());
                    if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Flee)
                    {
                        if (_r.IsReady() && Logic.SurviLogic.DangerValue > 10000 && AutoWalker.P.HealthPercent < 20)
                        {
                            AIHeroClient champToUlt =
                                EntityManager.Heroes.Enemies.FirstOrDefault(
                                    en =>
                                        en.HealthPercent > 5 && en.Distance(AutoWalker.P) < 600 &&
                                        en.Distance(AutoWalker.P) > 100);
                            if (champToUlt != null)
                            {
                                _r.Cast(Prediction.Position.PredictUnitPosition(champToUlt, 500).To3D());
                            }
                        }
                    }

                }

            }



            if (_r.IsReady() && AutoWalker.P.HealthPercent < 15)
            {
                AIHeroClient champToUlt =
                    EntityManager.Heroes.Enemies.FirstOrDefault(
                        en => en.Distance(AutoWalker.P) < 700);
                if (champToUlt != null)
                {
                    _r.Cast(champToUlt);
                }

            }

        }

        public int[] SkillSequence { get; private set; }
        public LogicSelector Logic { get; set; }


        public string ShopSequence { get; private set; }

        public void Harass(AIHeroClient target)
        {
        }

        public void Survi()
        {

        }

        public void Combo(AIHeroClient target)
        {

        }

        private static float TimeForAttack(Obj_AI_Base o, float range)
        {
            float time = (range - AutoWalker.P.Distance(o)) / (o.MoveSpeed + 100 - AutoWalker.P.MoveSpeed);
            float time2 = (AutoWalker.P.Distance(o.GetNearestTurret()) - 950) / (o.MoveSpeed + 100 - AutoWalker.P.MoveSpeed);
            return time < time2 ? time : time2;
        }
        private float EstDmg(Obj_AI_Base o, float time)
        {

            float eCd = _e.Handle.CooldownExpires - Game.Time < 0 ? 0 : _e.Handle.CooldownExpires - Game.Time;
            float qCd = _q.Handle.CooldownExpires - Game.Time < 0 ? 0 : _q.Handle.CooldownExpires - Game.Time;
            float eTimes = (float)Math.Floor((time - eCd) / .5f);
            float damage = AutoWalker.P.GetSpellDamage(o, SpellSlot.E) * eTimes;
            damage += AutoWalker.P.GetSpellDamage(o, SpellSlot.Q) * (float)Math.Floor((time - qCd) / _q.Handle.Cooldown);
            float neededMana = _e.Handle.SData.Mana * eTimes + _q.Handle.SData.Mana;
            if (AutoWalker.P.Mana < neededMana)
                return damage * AutoWalker.P.Mana / neededMana;
            return damage;
        }
    }
}