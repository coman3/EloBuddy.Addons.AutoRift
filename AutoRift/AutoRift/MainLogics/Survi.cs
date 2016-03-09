using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.MainLogics
{
    internal class Survi
    {
        private readonly LogicSelector _current;
        private bool _active;
        public float DangerValue;
        private int _hits;
        private int _hits2;
        private LogicSelector.MainLogics _returnTo;
        private float _spierdalanko;

        public Survi(LogicSelector currentLogic)
        {
            _current = currentLogic;
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            DecHits();
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender == null || args.Target == null) return;
            if (!args.Target.IsMe) return;
            if (sender.IsAlly) return;
            if (sender.Type == GameObjectType.obj_AI_Turret)
                SetSpierdalanko((1100 - AutoWalker.P.Distance(sender)) / AutoWalker.P.MoveSpeed);
            else if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                _hits++;
                _hits2++;
            }
            else if (sender.Type == GameObjectType.AIHeroClient) _hits += 2;
        }


        private void SetSpierdalanko(float sec)
        {
            _spierdalanko = Game.Time + sec;
            if (_active || (_current.Current == LogicSelector.MainLogics.CombatLogic && AutoWalker.P.HealthPercent() > 13))
                return;
            LogicSelector.MainLogics returnT = _current.SetLogic(LogicSelector.MainLogics.SurviLogic);
            if (returnT != LogicSelector.MainLogics.SurviLogic) _returnTo = returnT;
        }

        private void SetSpierdalankoUnc(float sec)
        {
            _spierdalanko = Game.Time + sec;
            if (_active) return;
            LogicSelector.MainLogics returnT = _current.SetLogic(LogicSelector.MainLogics.SurviLogic);
            if (returnT != LogicSelector.MainLogics.SurviLogic) _returnTo = returnT;
        }

        public void Activate()
        {
            if (_active) return;
            _active = true;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(250, 10, Color.Gold,
                "Survi, active: " + _active + "  hits: " + _hits + "  dangervalue: " + DangerValue);
        }

        public void Deactivate()
        {
            _active = false;
        }

        private void Game_OnTick(EventArgs args)
        {
            if (AutoWalker.P.HealthPercent<15&&AutoWalker.Ignite != null && AutoWalker.Ignite.IsReady())
            {
                AIHeroClient i = EntityManager.Heroes.Enemies.FirstOrDefault(en => en.Health < 50 + 20*AutoWalker.P.Level&&en.Distance(AutoWalker.P)<600);
                if (i != null) AutoWalker.UseIgnite(i);
            }
            if (_hits * 20 > AutoWalker.P.HealthPercent() || (_hits2 >= 5 && AutoWalker.P.Level < 8 && AutoWalker.P.HealthPercent < 50 && !EntityManager.Heroes.Enemies.Any(en => en.IsVisible() && en.HealthPercent < 10 && en.Distance(AutoWalker.P) < _current.MyChamp.OptimalMaxComboDistance)))
            {
                SetSpierdalanko(.5f);
            }
            DangerValue = _current.LocalAwareness.LocalDomination(AutoWalker.P);
            if (DangerValue > -2000 || AutoWalker.P.Distance(AutoWalker.EnemyLazer) < 1500)
            {
                SetSpierdalankoUnc(.5f);
                _current.SaveMylife = true;
            }
            if (!_active)
            {
                return;
            }
            if (ObjectManager.Player.HealthPercent() < 43)
            {
                AutoWalker.UseHPot();
            }
            if (Game.Time > _spierdalanko)
            {
                _current.SaveMylife = false;
                _current.SetLogic(_returnTo);
            }
            Vector3 enemyTurret = AutoWalker.P.GetNearestTurret().Position;
            
            Vector3 closestSafePoint;
            if (AutoWalker.P.Distance(enemyTurret) > 1200)
            {
                closestSafePoint = AutoWalker.P.GetNearestTurret(false).Position;
                if (closestSafePoint.Distance(AutoWalker.P) > 2000)
                {
                    AIHeroClient ally = EntityManager.Heroes.Allies.Where(
                        a =>
                            a.Distance(AutoWalker.P) < 1500 &&
                            _current.LocalAwareness.LocalDomination(a.Position) < -40000)
                        .OrderBy(al => al.Distance(AutoWalker.P))
                        .FirstOrDefault();
                    if (ally != null)
                        closestSafePoint = ally.Position;
                }
                if (closestSafePoint.Distance(AutoWalker.P) > 150)
                {
                    AIHeroClient ene =
                        EntityManager.Heroes.Enemies
                            .FirstOrDefault(en => en.Health > 0 && en.Distance(closestSafePoint) < 300);
                    if (ene != null)
                    {
                        closestSafePoint = AutoWalker.MyNexus.Position;
                    }
                }
                AutoWalker.SetMode(AutoWalker.P.Distance(closestSafePoint) < 200
                    ? Orbwalker.ActiveModes.Combo
                    : Orbwalker.ActiveModes.Flee);
                AutoWalker.WalkTo(closestSafePoint.Extend(AutoWalker.MyNexus, 200).To3DWorld());
            }
            else
            {
                AutoWalker.WalkTo(AutoWalker.P.Position.Away(enemyTurret, 1200));
                AutoWalker.SetMode(Orbwalker.ActiveModes.Flee);
            }
            if (AutoWalker.P.HealthPercent < 10)
            {
                if (AutoWalker.P.HealthPercent < 7)
                {
                    AutoWalker.UseBarrier();
                    AutoWalker.UseSeraphs();
                }
                AutoWalker.UseHeal();
            }
            if (EntityManager.Heroes.Enemies.Any(en => en.IsVisible() && en.Distance(AutoWalker.P) < 600))
            {
                if (AutoWalker.P.HealthPercent < 30)
                    AutoWalker.UseSeraphs();
                if (AutoWalker.P.HealthPercent < 25)
                    AutoWalker.UseBarrier();
                if (AutoWalker.P.HealthPercent < 18)
                    AutoWalker.UseHeal();
            }

            if (AutoWalker.Ghost!=null&&AutoWalker.Ghost.IsReady() && DangerValue > 20000)
                AutoWalker.UseGhost();
            if (DangerValue > 10000)
            {
                if (AutoWalker.P.HealthPercent < 45)
                    AutoWalker.UseSeraphs();
                if (AutoWalker.P.HealthPercent < 30)
                    AutoWalker.UseBarrier();
                if (AutoWalker.P.HealthPercent < 25)
                    AutoWalker.UseHeal();
            }
            _current.MyChamp.Survi();
        }

        private void DecHits()
        {
            if (_hits > 3)
                _hits = 3;
            if (_hits > 0)
                _hits--;
            _hits2--;
            Core.DelayAction(DecHits, 600);
        }
    }
}