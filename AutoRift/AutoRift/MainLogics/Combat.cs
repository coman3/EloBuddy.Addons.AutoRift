using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace AutoRift.MainLogics
{
    internal class Combat
    {
        private readonly LogicSelector _current;
        private bool _active;
        private string _lastMode = " ";
        private LogicSelector.MainLogics _returnTo;

        public Combat(LogicSelector currentLogic)
        {
            _current = currentLogic;
            Game.OnTick += Game_OnTick;
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(250, 25, System.Drawing.Color.Gold,
                "Combat, active:  " + _active + " last mode: " + _lastMode);
        }


        public void Activate()
        {
            if (_active) return;
            _active = true;
        }

        public void Deactivate()
        {
            _active = false;
        }

        private void Game_OnTick(EventArgs args)
        {
            if (_current.Current == LogicSelector.MainLogics.SurviLogic) return;
            AIHeroClient har = null;
            AIHeroClient victim = null;
            if (_current.SurviLogic.DangerValue < -15000)
                victim = EntityManager.Heroes.Enemies.Where(
                    vic => !vic.IsZombie &&
                        vic.Distance(AutoWalker.P) < vic.BoundingRadius + AutoWalker.P.AttackRange + 450 &&
                        vic.IsVisible() && vic.Health > 0 &&
                        _current.LocalAwareness.MyStrength()/_current.LocalAwareness.HeroStrength(vic) > 1.5)
                    .OrderBy(v => v.Health)
                    .FirstOrDefault();

            
            if (victim == null || AutoWalker.P.GetNearestTurret().Distance(AutoWalker.P) > 1100)
            {
                har =
                    EntityManager.Heroes.Enemies.Where(
                        h => !h.IsZombie &&
                            h.Distance(AutoWalker.P) < AutoWalker.P.AttackRange + h.BoundingRadius + 50 && h.IsVisible() &&
                            h.HealthPercent() > 0).OrderBy(h => h.Distance(AutoWalker.P)).FirstOrDefault();
            }


            if ((victim != null || har != null) && !_active)
            {
                LogicSelector.MainLogics returnT = _current.SetLogic(LogicSelector.MainLogics.CombatLogic);
                if (returnT != LogicSelector.MainLogics.CombatLogic) _returnTo = returnT;
            }
            if (!_active)
                return;
            if (victim == null && har == null)
            {
                _current.SetLogic(_returnTo);
                return;
            }
            if (victim != null)
            {
                _current.MyChamp.Combo(victim);
                Vector3 vicPos = Prediction.Position.PredictUnitPosition(victim, 500).To3D();

                Vector3 posToWalk =
                    AutoWalker.P.Position.Away(vicPos,
                        (victim.BoundingRadius + _current.MyChamp.OptimalMaxComboDistance - 30)*
                        Math.Min(_current.LocalAwareness.HeroStrength(victim)/_current.LocalAwareness.MyStrength()*1.6f, 1));
                        
                if (NavMesh.GetCollisionFlags(posToWalk).HasFlag(CollisionFlags.Wall))
                {
                    posToWalk =
                        vicPos.Extend(_current.PushLogic.MyTurret,
                            (victim.BoundingRadius + AutoWalker.P.AttackRange - 30)*
                            Math.Min(
                                _current.LocalAwareness.HeroStrength(victim)/_current.LocalAwareness.MyStrength()*2f, 1))
                            .To3DWorld();
                }

                Obj_AI_Turret nearestEnemyTurret = posToWalk.GetNearestTurret();

                if (victim.Health < 10 + 4 * AutoWalker.P.Level && EntityManager.Heroes.Allies.Any(al=>!al.IsDead()&&al.Distance(vicPos)<550))
                    AutoWalker.UseIgnite(victim);
                if (victim.Health + victim.HPRegenRate * 2.5f < 50 + 20 * AutoWalker.P.Level && vicPos.Distance(nearestEnemyTurret)<1350)
                    AutoWalker.UseIgnite(victim);
                _lastMode = "combo";
                if (AutoWalker.P.Distance(nearestEnemyTurret) < 950 + AutoWalker.P.BoundingRadius)
                {

                    if (victim.Health > AutoWalker.P.GetAutoAttackDamage(victim) + 15 ||
                        victim.Distance(AutoWalker.P) > AutoWalker.P.AttackRange + victim.BoundingRadius - 20)
                    {


                        _lastMode = "enemy under turret, ignoring";
                        _current.SetLogic(_returnTo);
                        return;
                    }
                    _lastMode = "combo under turret";
                }
                Orbwalker.DisableAttacking = _current.MyChamp.MaxDistanceForAa <
                                             AutoWalker.P.Distance(victim) + victim.BoundingRadius+10;
                AutoWalker.SetMode(Orbwalker.ActiveModes.Combo);
                AutoWalker.WalkTo(posToWalk);


                if (AutoWalker.Ghost != null && AutoWalker.Ghost.IsReady() &&
                    AutoWalker.P.HealthPercent()/victim.HealthPercent() > 2 &&
                    victim.Distance(AutoWalker.P) > AutoWalker.P.AttackRange + victim.BoundingRadius + 150 &&
                    victim.Distance(victim.Position.GetNearestTurret()) > 1500)
                    AutoWalker.Ghost.Cast();

                if (ObjectManager.Player.HealthPercent() < 35)
                {
                    if (AutoWalker.P.HealthPercent < 25)
                        AutoWalker.UseSeraphs();
                    if (AutoWalker.P.HealthPercent < 20)
                        AutoWalker.UseBarrier();

                        AutoWalker.UseHPot();
                }
            }
            else
            {
                Vector3 harPos = Prediction.Position.PredictUnitPosition(har, 500).To3D();
                harPos = AutoWalker.P.Position.Away(harPos, _current.MyChamp.HarassDistance + har.BoundingRadius - 20);
                
                _lastMode = "harass";
                Obj_AI_Turret tu = harPos.GetNearestTurret();
                AutoWalker.SetMode(Orbwalker.ActiveModes.Harass);
                if (harPos.Distance(tu) < 1000)
                {
                    if (harPos.Distance(tu) < 850 + AutoWalker.P.BoundingRadius)
                        AutoWalker.SetMode(Orbwalker.ActiveModes.Flee);
                    harPos = AutoWalker.P.Position.Away(tu.Position, 1090);
                    
                    _lastMode = "harass under turret";

                    /*if (harPos.Distance(AutoWalker.MyNexus) > tu.Distance(AutoWalker.MyNexus))
                        harPos =
                            tu.Position.Extend(AutoWalker.MyNexus, 1050 + AutoWalker.p.BoundingRadius).To3DWorld();*/
                }


                _current.MyChamp.Harass(har);


                AutoWalker.WalkTo(harPos);
            }
        }
    }
}