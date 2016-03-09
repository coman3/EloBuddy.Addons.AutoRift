using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace AutoRift.Utilities.AutoLvl
{
    internal class LvlSlider
    {
        private readonly CustomLvlSeq _cus;
        private readonly string[] _skills;
        private readonly Slider _s;
        private readonly int _level;
        public SkillToLvl Skill {
            get { return (SkillToLvl) (_s.CurrentValue==5?0:_s.CurrentValue); }
            set
            {
                
                _s.CurrentValue = (int)value;
            }
        }
        public LvlSlider(Menu menu, int level, CustomLvlSeq cus)
        {
            this._level = level;
            _s = new Slider(" ", 0, 0, 5);
            this._cus = cus;
            level += 1;
            _skills =new string[]
            {
                "Level "+level+": Not set", "Level "+level+": Q", "Level "+level+": W", "Level "+level+": E", "Level "+level+": R", "Level "+level+": Not set"
            };
            _s.DisplayName = _skills[_s.CurrentValue];
            _s.OnValueChange += s_OnValueChange;

            menu.Add(level + AutoWalker.P.ChampionName+"d", _s);
        }

        private void s_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {

            if (args.NewValue!=5&&args.NewValue!=0&&!_cus.CanLvl((SkillToLvl)args.NewValue, _level))
            {

                _s.CurrentValue = 0;
                

            }
            else
            {
                _cus.SetSkill(_level, (SkillToLvl)args.NewValue);
                sender.DisplayName = _skills[args.NewValue];
            }

                

            

        }
    }
}
