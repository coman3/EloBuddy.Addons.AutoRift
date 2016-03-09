using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoRift.Utilities.AutoShop;
using AutoRift.Utilities.Pathfinder;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift
{
    internal static class AutoWalker
    {
        public static string GameId;
        public static Spell.Active Ghost, Barrier, Heal, Recall;
        public static Spell.Skillshot Flash;
        public static Spell.Targeted Teleport, Ignite, Smite, Exhaust;
        public static readonly Obj_HQ MyNexus;
        public static readonly Obj_HQ EnemyNexus;
        public static readonly AIHeroClient P;
        public static readonly Obj_AI_Turret EnemyLazer;
        private static Orbwalker.ActiveModes _activeMode = Orbwalker.ActiveModes.None;
        private static InventorySlot _seraphs;
        private static int _hpSlot;
        private static readonly ColorBGRA color;
        private static List<Vector3> _pfNodes;
        private static readonly NavGraph NavGraph;
        private static bool _oldWalk;
        public static bool NewPf;
        private static bool _recalling;
        public static EventHandler EndGame;
        static AutoWalker()
        {
            GameId = DateTime.Now.Ticks + ""+RandomString(10);
            NewPf = MainMenu.GetMenu("AB").Get<CheckBox>("newPF").CurrentValue;
            NavGraph=new NavGraph(Path.Combine(SandboxConfig.DataDirectory, "AutoRift"));
            _pfNodes=new List<Vector3>();
            color = new ColorBGRA(79, 219, 50, 255);
            MyNexus = ObjectManager.Get<Obj_HQ>().First(n => n.IsAlly);
            EnemyNexus = ObjectManager.Get<Obj_HQ>().First(n => n.IsEnemy);
            EnemyLazer =
                ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => !tur.IsAlly && tur.GetLane() == Lane.Spawn);
            P = ObjectManager.Player;
            InitSummonerSpells();

            Target = ObjectManager.Player.Position;
            Orbwalker.DisableMovement = false;

            Orbwalker.DisableAttacking = false;
            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OverrideOrbwalkPosition = () => Target;
            if (Orbwalker.HoldRadius > 130 || Orbwalker.HoldRadius < 80)
            {
                Chat.Print("=================WARNING=================", Color.Red);
                Chat.Print("Your hold radius value in orbwalker isn't optimal for AutoRift", Color.Aqua);
                Chat.Print("Please set hold radius through menu=>Orbwalker");
                Chat.Print("Recommended values: Hold radius: 80-130, Delay between movements: 100-250");
            }
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
            
            Core.DelayAction(OnEndGame, 20000);
            UpdateItems();
            OldOrbwalk();
            EloBuddy.SDK.Events.Teleport.OnTeleport += Teleport_OnTeleport;
            Game.OnTick += OnTick;
        }

        private static void Teleport_OnTeleport(Obj_AI_Base sender, EloBuddy.SDK.Events.Teleport.TeleportEventArgs args)
        {
            if (sender.NetworkId==P.NetworkId && args.Type == TeleportType.Recall)
            {
                _recalling = args.Status == TeleportStatus.Start;
            }
        }

        public static bool Recalling()
        {
            return _recalling;
        }

        private static void OnEndGame()
        {

            if (MyNexus != null && EnemyNexus != null && (MyNexus.Health > 1) && (EnemyNexus.Health > 1))
            {
                Core.DelayAction(OnEndGame, 5000);
                return;
            }

            if (EndGame != null)
                EndGame(null, new EventArgs());

            if (MainMenu.GetMenu("AB").Get<CheckBox>("autoclose").CurrentValue)
            {
                Chat.Print("Closing game in 3 seconds...");
                Core.DelayAction(() =>
                {

                    Game.QuitGame();

                }, 3500);
            }

        }

        public static Vector3 Target { get; private set; }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (_activeMode == Orbwalker.ActiveModes.LaneClear)
            {
                Orbwalker.ActiveModesFlags = (P.TotalAttackDamage < 150 &&
                    EntityManager.MinionsAndMonsters.EnemyMinions.Any(
                        en =>
                            en.Distance(P) < P.AttackRange + en.BoundingRadius &&
                            Prediction.Health.GetPrediction(en, 2000) < P.GetAutoAttackDamage(en))
                        ) ? Orbwalker.ActiveModes.Harass
                        : Orbwalker.ActiveModes.LaneClear;
            }
            else
                Orbwalker.ActiveModesFlags = _activeMode;
        }

        public static void SetMode(Orbwalker.ActiveModes mode)
        {
            if (_activeMode != Orbwalker.ActiveModes.Combo)
                Orbwalker.DisableAttacking = false;
            _activeMode = mode;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Circle.Draw(color,40, Target );
            for (int i = 0; i < _pfNodes.Count-1; i++)
            {
                if(_pfNodes[i].IsOnScreen()||_pfNodes[i+1].IsOnScreen())
                    Line.DrawLine(Color.Aqua, 4, _pfNodes[i], _pfNodes[i+1]);
            }
        
        }

        public static void WalkTo(Vector3 tgt)
        {
            if (!NewPf)
            {
                Target = tgt;
                return;
            }

            if (_pfNodes.Any())
            {
                float dist = tgt.Distance(_pfNodes[_pfNodes.Count - 1]);
                if ( dist>900|| dist > 300&&P.Distance(tgt)<2000)
                {
                    _pfNodes = NavGraph.FindPathRandom(P.Position, tgt);
                }
                else
                {
                    _pfNodes[_pfNodes.Count - 1] = tgt;
                }
                Target = _pfNodes[0];
            }
            else
            {
                if (tgt.Distance(P) > 900)
                {
                    _pfNodes = NavGraph.FindPathRandom(P.Position, tgt);
                    Target = _pfNodes[0];
                }
                else
                {
                    Target = tgt;
                }
            }
        }




        private static void UpdateItems()
        {
            _hpSlot = BrutalItemInfo.GetHPotionSlot();
            _seraphs = P.InventoryItems.FirstOrDefault(it => (int)it.Id == 3040);
            Core.DelayAction(UpdateItems, 5000);
            
        }
        public static void UseSeraphs()
        {
            if (_seraphs != null && _seraphs.CanUseItem())
                _seraphs.Cast();
        }
        public static void UseGhost()
        {
            if (Ghost != null && Ghost.IsReady())
                Ghost.Cast();
        }
        public static void UseHPot()
        {
            if (_hpSlot == -1) return;
            P.InventoryItems[_hpSlot].Cast();
            _hpSlot = -1;
        }
        public static void UseBarrier()
        {
            if (Barrier != null && Barrier.IsReady())
                Barrier.Cast();
        }
        public static void UseHeal()
        {
            if (Heal != null && Heal.IsReady())
                Heal.Cast();
        }
        public static void UseIgnite(AIHeroClient target = null)
        {
            if (Ignite == null || !Ignite.IsReady()) return;
            if (target == null)target =
                    EntityManager.Heroes.Enemies.Where(en => en.Distance(P) < 600 + en.BoundingRadius)
                        .OrderBy(en => en.Health)
                        .FirstOrDefault();
            if (target != null && P.Distance(target) < 600 + target.BoundingRadius)
            {
                Ignite.Cast(target);
            }
                
        }

        private static void InitSummonerSpells()
        {
            Recall=new Spell.Active(SpellSlot.Recall);
            Barrier = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerbarrier")) == null ? null : new Spell.Active(ObjectManager.Player.GetSpellSlotFromName("summonerbarrier"));
            Ghost = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerhaste")) == null ? null : new Spell.Active(ObjectManager.Player.GetSpellSlotFromName("summonerhaste"));
            Flash = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerflash")) == null ? null : new Spell.Skillshot(ObjectManager.Player.GetSpellSlotFromName("summonerflash"), 600, SkillShotType.Circular);
            Heal = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerheal")) == null ? null : new Spell.Active(ObjectManager.Player.GetSpellSlotFromName("summonerheal"), 600);
            Ignite = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerdot")) == null ? null : new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);
            Exhaust = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerexhaust")) == null ? null : new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerexhaust"), 600);
            Teleport = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("summonerteleport")) == null ? null : new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerteleport"), int.MaxValue);
            Smite = Player.Spells.FirstOrDefault(sp => sp.SData.Name.Contains("smite")) == null ? null : new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("smite"), 600);
        }



#region old orbwalking, for those with not working orbwalker

        private static int _maxAdditionalTime = 50;
        private static int _adjustAnimation = 20;
        private static float _holdRadius = 50;
        private static float _movementDelay = .25f;

        private static float _nextMove;



        private static void OldOrbwalk()
        {

            if (!MainMenu.GetMenu("AB").Get<CheckBox>("oldWalk").CurrentValue) return;
            _oldWalk = true;
            Orbwalker.OnPreAttack+=Orbwalker_OnPreAttack;
        }


        private static void Orbwalker_OnPreAttack(AttackableUnit tgt, Orbwalker.PreAttackArgs args)
        {
            _nextMove = Game.Time + ObjectManager.Player.AttackCastDelay +
                       (Game.Ping + _adjustAnimation + RandGen.R.Next(_maxAdditionalTime)) / 1000f;
        }

        private static void OnTick(EventArgs args)
        {
            if (_pfNodes.Count != 0)
            {
                Target = _pfNodes[0];
                if (ObjectManager.Player.Distance(_pfNodes[0]) < 600)
                {
                    _pfNodes.RemoveAt(0);
                    
                }

            }



            if (!_oldWalk||ObjectManager.Player.Position.Distance(Target) < _holdRadius || Game.Time < _nextMove) return;
            _nextMove = Game.Time + _movementDelay;
            Player.IssueOrder(GameObjectOrder.MoveTo, Target, true);



        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }

#endregion





}