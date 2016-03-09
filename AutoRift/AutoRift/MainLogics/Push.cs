using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.MainLogics
{
    internal class Push
    {
        private readonly LogicSelector _currentLogic;
        private bool _active;
        private Obj_AI_Minion[] _currentWave;
        private int _currentWaveNum;
        private Lane _lane;

        private float _randomAngle;
        private float _randomExtend;
        private Vector3 _randomVector;
        private bool _wholeWave;

        private float _lastRand;
        private Vector3 _rand;
        private ColorBGRA _color;
        private ColorBGRA _colorGreen;
        private ColorBGRA _colorRed;
        private float _lastAtk;


        public Push(LogicSelector current)
        {
            _color=new ColorBGRA(255, 210, 105, 255);
            _colorRed = new ColorBGRA(139, 0, 0, 255);
            _colorGreen = new ColorBGRA(0, 100, 0, 255);
            SetRandVector();
            _randomVector = new Vector3();
            _currentWave = new Obj_AI_Minion[0];
            _currentLogic = current;
            Core.DelayAction(SetWaveNumber, 500);
            SetCurrentWave();
            SetOffset();
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
        }

        public Obj_AI_Base MyTurret { get; private set; }
        public Obj_AI_Base EnemyTurret { get; private set; }

        private void SetRandVector()
        {
            _randomVector.X = RandGen.R.NextFloat(0, 300);
            _randomVector.Y = RandGen.R.NextFloat(0, 300);
            Core.DelayAction(SetRandVector, 1000);
        }

        public void Reset(Obj_AI_Base myTower, Obj_AI_Base enemyTower, Lane ln)
        {
            Vector3 pingPos = AutoWalker.P.Distance(AutoWalker.MyNexus) - 100 > myTower.Distance(AutoWalker.MyNexus)
                ? enemyTower.Position
                : myTower.Position;
            Core.DelayAction(() => SafeFunctions.Ping(PingCategory.OnMyWay, pingPos.Randomized()), RandGen.R.Next(3000));
            _lane = ln;
            _currentWave = new Obj_AI_Minion[0];
            MyTurret = myTower;
            EnemyTurret = enemyTower;
            _randomExtend = 0;
            _currentLogic.SetLogic(LogicSelector.MainLogics.PushLogic);
        }

        public void Activate()
        {
            AutoWalker.SetMode(Orbwalker.ActiveModes.LaneClear);
            _currentLogic.Current = LogicSelector.MainLogics.PushLogic;
            if (_active) return;

            Game.OnTick += Game_OnTick;
            _active = true;
        }

        public void Deactivate()
        {
            _active = false;
            Game.OnTick -= Game_OnTick;
        }

        private void Game_OnTick(EventArgs args)
        {

            if (!_active||MyTurret==null) return;
            if (!AutoWalker.P.IsDead() && (MyTurret.Health <= 0 || EnemyTurret.Health <= 0))
            {
                _currentLogic.LoadLogic.SetLane();
            }
            if (_currentWave.Length == 0)
                UnderMyTurret();
            else if (AutoWalker.P.Distance(EnemyTurret) < 950 + AutoWalker.P.BoundingRadius)
                UnderEnemyTurret();
            else
                Between();
        }


        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(250, 40, Color.Gold,
                "Push, active: " + _active + "  wave num: " + _currentWaveNum + " minions left: " + _currentWave.Length);
            Circle.Draw(_color, 100, _currentWave.Length <= 0 ? AutoWalker.P.Position : AvgPos(_currentWave));

            if (MyTurret != null)
                Circle.Draw(_colorGreen, 200, MyTurret.Position);
            
            if (EnemyTurret != null)
                Circle.Draw(_colorRed, 200, EnemyTurret.Position);
        }

        private void UnderEnemyTurret()
        {
            if (
                ObjectManager.Get<Obj_AI_Minion>()
                    .Count(min => min.IsAlly && min.HealthPercent() > 30 && min.Distance(EnemyTurret) < 850) < 2 || (EntityManager.Heroes.Enemies.Any(en => en.IsVisible && en.HasBuffOfType(BuffType.Damage)&&AutoWalker.P.HealthPercent-en.HealthPercent<65 && en.Distance(EnemyTurret) < 800 && AutoWalker.P.Distance(EnemyTurret) < AutoWalker.P.BoundingRadius+850)))
            {
                AutoWalker.SetMode(Orbwalker.ActiveModes.LaneClear);
                AutoWalker.WalkTo(AutoWalker.P.Position.Away(EnemyTurret.Position, 1200));
                return;
            }
            if (AutoWalker.P.Distance(EnemyTurret) <
                AutoWalker.P.AttackRange + EnemyTurret.BoundingRadius + Orbwalker.HoldRadius &&
                AutoWalker.P.Distance(EnemyTurret) > AutoWalker.P.AttackRange)
            {
                AutoWalker.SetMode(Orbwalker.ActiveModes.None);
                if (Game.Time > _lastAtk)
                {
                    _lastAtk = Game.Time + RandGen.R.NextFloat(.2f, .4f);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, EnemyTurret);
                }
                
            }
            else
            {
                AutoWalker.SetMode(Orbwalker.ActiveModes.LastHit);
                AutoWalker.WalkTo(
                    EnemyTurret.Position.Extend(AutoWalker.P, AutoWalker.P.AttackRange + EnemyTurret.BoundingRadius)
                        .To3DWorld());
            }
        }

        private void Between()
        {
            AutoWalker.SetMode(Orbwalker.ActiveModes.LaneClear);
            Vector3 p = AvgPos(_currentWave);
            if (p.Distance(AutoWalker.MyNexus) > MyTurret.Distance(AutoWalker.MyNexus))
            {
                AIHeroClient ally =
                    EntityManager.Heroes.Allies.Where(
                        al => !al.IsMe &&
                              AutoWalker.P.Distance(al) < 1500 &&
                              al.Distance(EnemyTurret) < p.Distance(EnemyTurret) + 100 &&
                              _currentLogic.LocalAwareness.LocalDomination(al) < -10000)
                        .OrderBy(l => l.Distance(AutoWalker.P))
                        .FirstOrDefault();
                if (ally != null &&
                    Math.Abs(p.Distance(AutoWalker.EnemyNexus) - AutoWalker.P.Distance(AutoWalker.EnemyNexus)) < 200)
                    p = ally.Position.Extend(MyTurret, 160).To3DWorld() + _randomVector;
                p =
                    p.Extend(p.Extend(
                        AutoWalker.P.Distance(MyTurret) < AutoWalker.P.Distance(EnemyTurret)
                            ? MyTurret
                            : EnemyTurret,
                        400).To3D().RotatedAround(p, 1.57f), _randomExtend).To3DWorld();
                AutoWalker.WalkTo(p);
            }
            else
                UnderMyTurret();
        }

        private void UnderMyTurret()
        {
            if (AutoWalker.P.Gold <= 100 ||
                !EntityManager.MinionsAndMonsters.EnemyMinions.Any(en => en.Distance(MyTurret) < 1000))
            {
                if (Game.Time > _lastRand)
                {
                    _lastRand = Game.Time+RandGen.R.NextFloat(5, 10);
                    _rand = new Vector3(RandGen.R.NextFloat(-400, 400), RandGen.R.NextFloat(-400, 400), 0);
                    while ((MyTurret.Position.Extend(AutoWalker.MyNexus, 200).To3D() + _rand).Distance(MyTurret)<250)
                    {
                        _rand = new Vector3(RandGen.R.NextFloat(-400, 400), RandGen.R.NextFloat(-400, 400), 0);
                    }
                    
                }

                AutoWalker.WalkTo(MyTurret.Position.Extend(AutoWalker.MyNexus, 200).To3D()+_rand);
                return;
            }

            Vector3 p = new Vector3();
            AIHeroClient ally =
                EntityManager.Heroes.Allies.Where(
                    al => !al.IsMe &&
                          AutoWalker.P.Distance(al) < 1200 && al.Distance(EnemyTurret) < p.Distance(EnemyTurret) + 150 &&
                          _currentLogic.LocalAwareness.LocalDomination(al) < -15000)
                    .OrderBy(l => l.Distance(AutoWalker.P))
                    .FirstOrDefault();
            if (AutoWalker.P.Gold > 100 && ally != null)
            {
                p = ally.Position.Extend(MyTurret, 160).To3DWorld() + _randomVector;
                AutoWalker.SetMode(AutoWalker.P.Distance(EnemyTurret) < 900
                    ? Orbwalker.ActiveModes.LastHit
                    : Orbwalker.ActiveModes.LaneClear);
                AutoWalker.WalkTo(p);
            }
            else
            {
                AutoWalker.WalkTo(MyTurret.Position.Extend(AutoWalker.P.Position, 350 + _randomExtend/2)
                    .To3D()
                    .RotatedAround(MyTurret.Position, _randomAngle));
            }
        }

        private Vector3 AvgPos(Obj_AI_Minion[] objects)
        {
            double x = 0, y = 0;
            foreach (Obj_AI_Minion obj in objects)
            {
                x += obj.Position.X;
                y += obj.Position.Y;
            }
            return new Vector2((float) (x/objects.Count()), (float) (y/objects.Count())).To3DWorld();
        }

        private void SetOffset()
        {
            if (!_active)
            {
                Core.DelayAction(SetOffset, 500);
                return;
            }
            float newEx = _randomExtend;
            while (Math.Abs(newEx - _randomExtend) < 190)
            {
                newEx = RandGen.R.NextFloat(-400, 400);
            }
            _randomAngle = RandGen.R.NextFloat(0, 6.28f);
            _randomExtend = newEx;
            Core.DelayAction(SetOffset, RandGen.R.Next(800, 1600));
        }

        private void SetWaveNumber()
        {
            Core.DelayAction(SetWaveNumber, 500);
            Obj_AI_Minion closest =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(
                        min => min.IsAlly && min.Name.Length > 13 && min.GetLane() == _lane && min.HealthPercent() > 80)
                    .OrderBy(min => min.Distance(EnemyTurret))
                    .FirstOrDefault();
            if (closest != null)
            {
                _currentWaveNum = closest.GetWave();
            }
        }

        private void SetCurrentWave()
        {
            if (_currentWaveNum == 0)
            {
                Core.DelayAction(SetCurrentWave, 1000);
                return;
            }
            _currentWave =
                _currentWave.Where(
                    min => min.Health > 1)
                    .ToArray();
            if (_currentWave.Length > 1)
            {
                Core.DelayAction(SetCurrentWave, 1000);
                return;
            }

            Obj_AI_Minion[] newMinions =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(min => min.IsAlly && min.GetLane() == _lane && min.GetWave() == _currentWaveNum)
                    .ToArray();
            if (!_wholeWave && newMinions.Length < 7)
            {
                _wholeWave = true;

                Core.DelayAction(SetCurrentWave,
                    newMinions.Any(min => min.Distance(AutoWalker.MyNexus) < 800) ? 3000 : 300);
            }
            else
            {
                _wholeWave = false;
                _currentWave = newMinions;
                Core.DelayAction(SetCurrentWave, 1000);
            }
        }
    }
}