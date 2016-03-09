using System;
using System.Collections.Generic;
using AutoRift.MyChampLogic;
using AutoRift.Utilities;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoRift.MainLogics
{
    internal class LogicSelector
    {
        public readonly Combat CombatLogic;
        public readonly Load LoadLogic;
        public readonly LocalAwareness LocalAwareness;
        public readonly Push PushLogic;
        public readonly Recall RecallLogic;
        public readonly Surrender Surrender;
        public readonly Survi SurviLogic;


        public readonly IChampLogic MyChamp;
        public bool SaveMylife;

        public LogicSelector(IChampLogic my, Menu menu)
        {
            MyChamp = my;
            Current = MainLogics.Nothing;
            SurviLogic = new Survi(this);
            RecallLogic = new Recall(this, menu);
            PushLogic = new Push(this);
            LoadLogic = new Load(this);
            CombatLogic = new Combat(this);
            Surrender = new Surrender();

            Core.DelayAction(() => LoadLogic.SetLane(), 1000);
            LocalAwareness = new LocalAwareness();
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnEndScene += Drawing_OnDraw;
            MyChamp.Logic = this;
            AutoWalker.EndGame += End;
            Core.DelayAction(Watchdog, 3000);
        }

        public MainLogics Current { get; set; }

        private void Drawing_OnDraw(System.EventArgs args)
        {
            Drawing.DrawText(250, 85, Color.Gold, Current.ToString());
            Vector2 v = Game.CursorPos.WorldToScreen();
            Drawing.DrawText(v.X, v.Y - 20, Color.Gold, LocalAwareness.LocalDomination(Game.CursorPos) + " ");
        }

        public MainLogics SetLogic(MainLogics newlogic)
        {
            if (SaveMylife) return Current;
            if (newlogic != MainLogics.PushLogic)
                PushLogic.Deactivate();
            MainLogics old = Current;
            switch (Current)
            {
                case MainLogics.SurviLogic:
                    SurviLogic.Deactivate();
                    break;

                case MainLogics.RecallLogic:
                    RecallLogic.Deactivate();
                    break;
                case MainLogics.CombatLogic:
                    CombatLogic.Deactivate();
                    break;
            }


            switch (newlogic)
            {
                case MainLogics.PushLogic:
                    PushLogic.Activate();
                    break;
                case MainLogics.LoadLogic:
                    LoadLogic.Activate();
                    break;
                case MainLogics.SurviLogic:
                    SurviLogic.Activate();

                    break;
                case MainLogics.RecallLogic:
                    RecallLogic.Activate();
                    break;
                case MainLogics.CombatLogic:
                    CombatLogic.Activate();
                    break;
            }


            Current = newlogic;
            return old;
        }

        private void Watchdog()
        {
            Core.DelayAction(Watchdog, 500);
            if (Current == MainLogics.Nothing && !LoadLogic.Waiting)
            {
                Chat.Print("Hang detected");
                LoadLogic.SetLane();
            }
        }

        private void End(object o, EventArgs e)
        {
        }
        internal enum MainLogics
        {
            PushLogic,
            RecallLogic,
            LoadLogic,
            SurviLogic,
            CombatLogic,
            Nothing
        }
    }
}