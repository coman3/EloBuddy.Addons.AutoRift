using System;
using System.Linq;
using AutoRift.Utilities.AutoShop;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace AutoRift.MainLogics
{
    internal class Recall
    {
        private readonly Slider _flatGold, _goldPerLevel;
        private readonly LogicSelector _current;
        private readonly Obj_SpawnPoint _spawn;
        private bool _active;
        private GrassObject _g;
        //private float lastRecallGold;
        private float _lastRecallTime;
        private int _recallsWithGold; //TODO repair shop and remove this tempfix

        public Recall(LogicSelector currentLogic, Menu parMenu)
        {
            Menu menu = parMenu.AddSubMenu("Recall settings", "ergtrh");
            _flatGold=new Slider("Minimum base gold to recall", 560, 0, 4000);
            _goldPerLevel = new Slider("Minmum gold per level to recall", 70, 0, 300);
            menu.Add("mingold", _flatGold);
            menu.Add("goldper", _goldPerLevel);
            menu.AddSeparator(100);
            menu.AddLabel(
    @"
Example: Your champ has lvl 10
Base gold = 560
Gold per level = 70
Minimum gold = 560+70*10 = 1260

AutoRift won't recall if you have less gold than needed for next item.

            ");
            _current = currentLogic;
            foreach (
                Obj_SpawnPoint so in
                    ObjectManager.Get<Obj_SpawnPoint>().Where(so => so.Team == ObjectManager.Player.Team))
            {
                _spawn = so;
            }
            Core.DelayAction(ShouldRecall, 3000);
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
        }


        private void ShouldRecall()
        {
            if (_active)
            {
                Core.DelayAction(ShouldRecall, 500);
                return;
            }
            if (_current.Current == LogicSelector.MainLogics.CombatLogic)
            {
                Core.DelayAction(ShouldRecall, 500);
                return;
            }

            if ((AutoWalker.P.Gold > _flatGold.CurrentValue+AutoWalker.P.Level*_goldPerLevel.CurrentValue&&AutoWalker.P.Gold>ShopGlobals.GoldForNextItem && AutoWalker.P.InventoryItems.Length < 8 &&
                 _recallsWithGold <= 30) || AutoWalker.P.HealthPercent() < 25)
            {
                if (AutoWalker.P.Gold > (AutoWalker.P.Level + 2)*150 && AutoWalker.P.InventoryItems.Length < 8 &&
                    _recallsWithGold <= 30)
                    _recallsWithGold++;
                _current.SetLogic(LogicSelector.MainLogics.RecallLogic);
            }
            Core.DelayAction(ShouldRecall, 500);
        }

        public void Activate()
        {
            if (_active) return;
            _active = true;
            _g = null;
            Game.OnTick += Game_OnTick;
        }

        public void Deactivate()
        {
            _lastRecallTime = 0;
            _active = false;
            Game.OnTick -= Game_OnTick;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(250, 55, System.Drawing.Color.Gold,
                "Recall, active: " + _active+" next item: "+ShopGlobals.Next+" gold needed:"+ShopGlobals.GoldForNextItem);
        }

        private void Game_OnTick(EventArgs args)
        {
            AutoWalker.SetMode(Orbwalker.ActiveModes.Combo);
            if (ObjectManager.Player.Distance(_spawn) < 400 && ObjectManager.Player.HealthPercent() > 85 &&
                (ObjectManager.Player.ManaPercent > 80 || ObjectManager.Player.PARRegenRate <= .0001))

                _current.SetLogic(LogicSelector.MainLogics.PushLogic);
            else if (ObjectManager.Player.Distance(_spawn) < 2000)
                AutoWalker.WalkTo(_spawn.Position);
            else if (!ObjectManager.Player.IsRecalling() && Game.Time > _lastRecallTime)
            {
                Obj_AI_Turret nearestTurret =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .Where(t => t.Team == ObjectManager.Player.Team && !t.IsDead())
                        .OrderBy(t => t.Distance(ObjectManager.Player))
                        .First();
                Vector3 recallPos = nearestTurret.Position.Extend(_spawn, 300).To3DWorld();
                if (AutoWalker.P.HealthPercent() > 35)
                {
                    if (_g == null)
                    {

                        _g = ObjectManager.Get<GrassObject>()
                            .Where(gr => gr.Distance(AutoWalker.MyNexus) < AutoWalker.P.Distance(AutoWalker.MyNexus)&&gr.Distance(AutoWalker.P)>Orbwalker.HoldRadius)
                            .OrderBy(gg => gg.Distance(AutoWalker.P)).FirstOrDefault(gr => ObjectManager.Get<GrassObject>().Count(gr2=>gr.Distance(gr2)<65)>=4);
                    }
                    if (_g != null && _g.Distance(AutoWalker.P) < nearestTurret.Position.Distance(AutoWalker.P))
                    {
                        AutoWalker.SetMode(Orbwalker.ActiveModes.Flee);
                        recallPos = _g.Position;
                    }
                }

                if ((!AutoWalker.P.IsMoving && ObjectManager.Player.Distance(recallPos) < Orbwalker.HoldRadius + 30) || (AutoWalker.P.IsMoving && ObjectManager.Player.Distance(recallPos) < 30))
                {
                    CastRecall();
                }
                else
                    AutoWalker.WalkTo(recallPos);
            }
        }

        private void CastRecall()
        {
            if (Game.Time < _lastRecallTime || AutoWalker.Recalling() || ObjectManager.Player.Distance(_spawn) < 500) return;
            _lastRecallTime = Game.Time + 2f;
            Core.DelayAction(CastRecall2, 300);
        }
        private void CastRecall2()//Kappa
        {
            if (AutoWalker.Recalling() || ObjectManager.Player.Distance(_spawn) < 500) return;
            _lastRecallTime = Game.Time + 2f;
            AutoWalker.SetMode(Orbwalker.ActiveModes.None);
            ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall);
        }
    }
}