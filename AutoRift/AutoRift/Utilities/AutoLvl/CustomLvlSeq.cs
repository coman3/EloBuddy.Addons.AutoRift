using System;
using System.IO;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace AutoRift.Utilities.AutoLvl
{
    internal enum SkillToLvl
    {
        NotSet = 0,
        Q = 1,
        W = 2,
        E = 3,
        R = 4
    }


    internal class CustomLvlSeq
    {
        private readonly bool[] _locked;
        private readonly CheckBox _updater;
        private readonly AIHeroClient _champ;
        private readonly DefautSequences _def;
        private string _lvlFile;
        private readonly LvlSlider[] _sliders;
        private readonly SkillToLvl[] _skills;
        private readonly int _maxLvl;
        private readonly CheckBox _clear, _defau, _profile1, _profile2;
        private int _profile;
        private bool _sa;
        private readonly string _dir;
        private readonly string _se;
        private readonly Slider _humanMin, _humanMax;
        private readonly SkillLevelUp _lvlUp;

        public CustomLvlSeq(Menu m, AIHeroClient champ, string dir, string seq = "", int maxlvl = 18)
        {
            _locked = new bool[] { true };
            this._dir = dir;
            _se = seq;
            Menu menuSettings = m.AddSubMenu("Skill LvlUp", "AB_SL_SETTINGS");
            CheckBox enabled = new CheckBox("Enabled", true);
            menuSettings.AddGroupLabel("General");
            menuSettings.Add(champ + "enabled", enabled);
            menuSettings.AddSeparator(10);
            menuSettings.AddGroupLabel("Current profile");
            _profile1 = new CheckBox("Profile 1", true);
            _profile2 = new CheckBox("Profile 2", false);
            menuSettings.Add(champ.ChampionName + Game.MapId + "p1", _profile1);
            menuSettings.Add(champ.ChampionName + Game.MapId + "p2", _profile2);


            
            _updater = new CheckBox("Update default sequences");
            _clear = new CheckBox("Clear current profile", false);
            menuSettings.Add("clear", _clear);
            _defau = new CheckBox("Set current profile to default");
            menuSettings.Add("defaults", _defau);
            menuSettings.AddSeparator(10);

            menuSettings.AddGroupLabel("Humanizer");
            _humanMin = new Slider("Minimum time after level up to upgrade an ability(miliseconds)", 300, 0, 2000);
            _humanMax = new Slider("Maximum time after level up to upgrade an ability(miliseconds)", 500, 0, 2000);
            menuSettings.Add("xhm", _humanMin);
            menuSettings.Add("xhmx", _humanMax);
            _humanMin.OnValueChange += humanMin_OnValueChange;
            _humanMax.OnValueChange += humanMax_OnValueChange;
            menuSettings.AddSeparator(10);
            menuSettings.AddGroupLabel("Updater");
            menuSettings.Add("updateSkills", _updater);
            _updater.CurrentValue = false;
            _locked[0] = false;
            _updater.OnValueChange += updater_OnValueChange;
            _clear.CurrentValue = false;
            _clear.OnValueChange += clear_OnValueChange;
            _defau.CurrentValue = false;
            _defau.OnValueChange += defau_OnValueChange;
            _profile1.OnValueChange += profile1_OnValueChange;
            _profile2.OnValueChange += profile2_OnValueChange;

            _profile = _profile1.CurrentValue ? 1 : 2;
            _lvlFile = Path.Combine(dir + "\\" + "Skills-" + champ.ChampionName + "-" + Game.MapId +"-P"+_profile+ ".txt");

            this._champ = champ;
            _def = new DefautSequences(dir + "\\" + "Skills-DEFAULT.txt");
            _maxLvl = maxlvl;
            Menu menu = m.AddSubMenu("Skill sequence: " + champ.ChampionName);




            
            _sliders = new LvlSlider[maxlvl];
            _skills = new SkillToLvl[maxlvl];
            for (int i = 0; i < _maxLvl; i++)
            {
                _sliders[i] = new LvlSlider(menu, i, this);
            }
            Load(seq);
            _lvlUp=new SkillLevelUp(_skills, enabled)
            {
                MaxTime = _humanMax.CurrentValue,
                MinTime = _humanMin.CurrentValue
            };

        }

        void humanMax_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (args.NewValue < _humanMin.CurrentValue)
            {
                _humanMax.CurrentValue = _humanMin.CurrentValue;
            }
            _lvlUp.MaxTime = _humanMax.CurrentValue;
        }

        void humanMin_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (args.NewValue > _humanMax.CurrentValue)
            {
                _humanMin.CurrentValue=_humanMax.CurrentValue;
            }
            _lvlUp.MinTime = _humanMin.CurrentValue;
        }

        private void updater_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (_locked[0])
            {
                Core.DelayAction(() => _updater.CurrentValue = true, 1);

                return;
            }
            if (args.NewValue && !args.OldValue)
            {
                Core.DelayAction(() => _updater.CurrentValue = true, 1);
                _updater.DisplayName = "Updating...";
                _def.UpdateSequences(_locked);
                UnlockButton();
            }
        }

        private void UnlockButton()
        {
            if (!_locked[0])
            {
                _updater.CurrentValue = false;
                _updater.DisplayName = "Update default sequences";
                return;
            }
            Core.DelayAction(UnlockButton, 80);
        }



        private void profile1_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue)
            {
                Core.DelayAction(()=>_profile1.CurrentValue=true, 1);
                return;
            }

            _profile2.OnValueChange -= profile2_OnValueChange;
            _profile2.CurrentValue = false;
            _profile2.OnValueChange += profile2_OnValueChange;
            _profile = 1;
            _lvlFile = Path.Combine(_dir + "\\" + "Skills-" + _champ.ChampionName + "-" + Game.MapId + "-P" + _profile + ".txt");
            Load(_se);
        }

        private void profile2_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue)
            {
                Core.DelayAction(() => _profile2.CurrentValue = true, 1);
                return;
            }

            _profile1.OnValueChange -= profile1_OnValueChange;
            _profile1.CurrentValue = false;
            _profile1.OnValueChange += profile1_OnValueChange;
            _profile = 2;
            _lvlFile = Path.Combine(_dir + "\\" + "Skills-" + _champ.ChampionName + "-" + Game.MapId + "-P" + _profile + ".txt");
            Load(_se);
        }


        private void clear_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;
            ClearSeq();
            Core.DelayAction(() => { _clear.CurrentValue = false; }, 200);

        }

        private void defau_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (!args.NewValue) return;
            InitSeq(_def.GetDefaultSequence(_champ.Hero));
            if (File.Exists(_lvlFile))
            {
                File.Delete(_lvlFile);
            }
            Core.DelayAction(() => { _defau.CurrentValue = false; }, 200);

        }

        private void ClearSeq()
        {
            _sa = false;
            for (int i = 0; i < _maxLvl; i++)
            {
                _skills[i] = SkillToLvl.NotSet;
                _sliders[i].Skill = SkillToLvl.NotSet;

            }
            _sa = true;
            Save();
        }

        private void InitSeq(string seq)
        {

            for (int i = 0; i < _maxLvl; i++)
            {
                _skills[i] = SkillToLvl.NotSet;
            }
            _sa = false;
            if (string.IsNullOrEmpty(seq) || seq.Split(';').Length != _maxLvl)
            {
                for (int i = 0; i < _maxLvl; i++)
                {
                    _sliders[i].Skill = SkillToLvl.NotSet;
                }
            }
            else
            {
                try
                {

                    string[] s = seq.Split(';');

                    for (int i = 0; i < _maxLvl; i++)
                    {
                        _sliders[i].Skill = (SkillToLvl)Enum.Parse(typeof(SkillToLvl), s[i], true);
                    }
                    for (int i = 0; i < _maxLvl; i++)
                    {
                        _skills[i] = (SkillToLvl)Enum.Parse(typeof(SkillToLvl), s[i], true);
                    }
                }
                catch
                {
                    Chat.Print("Skill upgrader: couldn't load skill sequence, set it manually.");
                    for (int i = 0; i < _maxLvl; i++)
                    {
                        _sliders[i].Skill = SkillToLvl.NotSet;
                    }
                }

            }



            _sa = true;

        }

        private void Load(string seq)
        {
            InitSeq(File.Exists(_lvlFile) ? File.ReadAllText(_lvlFile) : (string.IsNullOrEmpty(seq) ? _def.GetDefaultSequence(_champ.Hero) : seq));
        }
        private void Save()
        {
            string s = string.Empty;
            for (int i = 0; i < _maxLvl; i++)
            {
                s += ";" + _skills[i];
            }
            File.WriteAllText(_lvlFile, s.Substring(1));
        }

        private int CountSkillLvl(SkillToLvl s, int level)
        {
            int lvl = 0;
            for (int i = 0; i < level; i++)
            {
                if (_skills[i] == s)
                    lvl++;
            }
            return lvl;
        }

        public bool CanLvl(SkillToLvl s, int level)
        {
            int q = CountSkillLvl(s, 18);
            if (s == SkillToLvl.R)
            {
                if (q >= 3) return false;
                if (level < 5) return CountSkillLvl(s, 5) < 0;
                if (level < 10) return CountSkillLvl(s, 10) < 1;
                if (level < 15) return CountSkillLvl(s, 15) < 2;
                return q < 3;
            }

            if (q >= 5) return false;
            if (level < 2) return CountSkillLvl(s, 2) < 1;
            if (level < 4) return CountSkillLvl(s, 4) < 2;
            if (level < 6) return CountSkillLvl(s, 6) < 3;
            if (level < 8) return CountSkillLvl(s, 8) < 4;
            return q < 5;
        }

        public void SetSkill(int level, SkillToLvl skill)
        {
            _skills[level] = skill;
            if (_sa)
                Save();
        }
    }
}
