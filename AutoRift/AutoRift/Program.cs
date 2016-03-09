using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using AutoRift.MainLogics;
using AutoRift.MyChampLogic;
using AutoRift.Utilities;
using AutoRift.Utilities.AutoLvl;
using AutoRift.Utilities.AutoShop;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Version = System.Version;

namespace AutoRift
{
    internal static class Program
    {
        private static Menu _menu;
        private static IChampLogic _myChamp;
        private static LogicSelector Logic { get; set; }

        public static void Main()
        {
            Hacks.RenderWatermark = false;

            ManagedTexture.OnLoad += args =>
            {
                args.Process = false;

            };
            Hacks.DisableTextures = true;
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            CreateFs();
            Chat.Print("AutoRift will start in 5 seconds. ");
            Core.DelayAction(Start, 5000);
            _menu = MainMenu.AddMenu("AutoRift", "AB");
            _menu.Add("sep1", new Separator(1));
            CheckBox c =
                new CheckBox("Call mid, will leave if other player stays on mid(only auto lane)", true);

            PropertyInfo property2 = typeof(CheckBox).GetProperty("Size");

            property2.GetSetMethod(true).Invoke(c, new object[] { new Vector2(500, 20) });
            _menu.Add("mid", c);

            Slider s = _menu.Add("lane", new Slider(" ", 1, 1, 4));
            string[] lanes =
            {
                "", "Selected lane: Auto", "Selected lane: Top", "Selected lane: Mid",
                "Selected lane: Bot"
            };
            s.DisplayName = lanes[s.CurrentValue];
            s.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = lanes[changeArgs.NewValue];
                };

            _menu.Add("disablepings", new CheckBox("Disable pings", false));
            _menu.Add("disablechat", new CheckBox("Disable chat", false));
            CheckBox newpf = new CheckBox("Use smart pathfinder", true);
            
            _menu.Add("newPF", newpf);
            newpf.OnValueChange += newpf_OnValueChange;
            CheckBox autoclose = new CheckBox("Auto close lol when the game ends. F5 to apply", false);
            property2.GetSetMethod(true).Invoke(autoclose, new object[] { new Vector2(500, 20) });
            _menu.AddSeparator(5);
            _menu.Add("autoclose", autoclose);
            _menu.AddSeparator(5);
            _menu.Add("sep2", new Separator(170));
            _menu.Add("oldWalk", new CheckBox("Use old orbwalking(press f5 after)", false));
            _menu.Add("reselectlane", new CheckBox("Reselect lane", false));
            _menu.Add("debuginfo", new CheckBox("Draw debug info(press f5 after)", true));
            _menu.Add("l1", new Label("By Christian Brutal Sniper"));
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            _menu.Add("l2",
                new Label("Version " + v.Major + "." + v.Minor + " Build time: " + v.Build % 100 + " " +
                          CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(v.Build / 100) + " " +
                          (v.Revision / 100).ToString().PadLeft(2, '0') + ":" +
                          (v.Revision % 100).ToString().PadLeft(2, '0')));

        }




        static void newpf_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            AutoWalker.NewPf = args.NewValue;
        }



        private static void Start()
        {
            Humanizers.RandGen.Start();
            bool generic = false;
            switch (ObjectManager.Player.Hero)
            {
                case Champion.Ashe:
                    _myChamp = new Ashe();
                    break;
                case Champion.Caitlyn:
                    _myChamp = new Caitlyn();
                    break;
                default:
                    generic = true;
                    _myChamp = new Generic();
                    break;
                case Champion.Ezreal:
                    _myChamp = new Ezreal();
                    break;
                case Champion.Cassiopeia:
                    _myChamp = new Cassiopeia();
                    break;
            }
            CustomLvlSeq cl = new CustomLvlSeq(_menu, AutoWalker.P, Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Skills"));
            if (!generic)
            {
                BuildCreator bc = new BuildCreator(_menu, Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Builds"), _myChamp.ShopSequence);
            }


            else
            {
                _myChamp = new Generic();
                if (MainMenu.GetMenu("AB_" + ObjectManager.Player.ChampionName) != null &&
                    MainMenu.GetMenu("AB_" + ObjectManager.Player.ChampionName).Get<Label>("shopSequence") != null)
                {
                    Chat.Print("AutoRift: Loaded shop plugin for " + ObjectManager.Player.ChampionName);
                    BuildCreator bc = new BuildCreator(_menu, Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Builds"),
                        MainMenu.GetMenu("AB_" + ObjectManager.Player.ChampionName)
                            .Get<Label>("shopSequence")
                            .DisplayName);
                }
                else
                {
                    BuildCreator bc = new BuildCreator(_menu, Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Builds"), _myChamp.ShopSequence);
                }
            }
            Logic = new LogicSelector(_myChamp, _menu);
        }

        private static void CreateFs()
        {
            Directory.CreateDirectory(Path.Combine(SandboxConfig.DataDirectory, "AutoRift"));

            Directory.CreateDirectory(Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Builds"));
            Directory.CreateDirectory(Path.Combine(SandboxConfig.DataDirectory, "AutoRift\\Skills"));
        }
    }
}