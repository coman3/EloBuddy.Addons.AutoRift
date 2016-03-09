using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace AutoRift.Utilities.AutoLvl
{
    internal class SkillLevelUp
    {
        private readonly CheckBox _enabled;
        private readonly SkillToLvl[] _skills;
        private readonly SpellDataInst _e = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
        private readonly SpellDataInst _q = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        private readonly SpellDataInst _r = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);
        private readonly SpellDataInst _w = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        public int MinTime=0;
        public int MaxTime=0;

        private int _oldLvl = 1;

        public SkillLevelUp(SkillToLvl[] skills, CheckBox enabled)
        {
            this._enabled = enabled;
            enabled.OnValueChange += enabled_OnValueChange;
            this._skills = skills;
            Core.DelayAction(() => OnLvLUp(ObjectManager.Player.Level), RandGen.R.Next(MinTime, MaxTime));
            //Obj_AI_Base.OnLevelUp += Player_OnLevelUp;TODO waiting for devs to fix onlvlup...
            Game.OnTick += Game_OnTick;
        }

        private void Game_OnTick(System.EventArgs args)
        {
            if (AutoWalker.P.Level <= _oldLvl) return;
            _oldLvl = AutoWalker.P.Level;
            OnLvLUp(_oldLvl);
        }

        private void enabled_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if(args.NewValue)
                OnLvLUp(ObjectManager.Player.Level, true);
        }

        private void Player_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (sender != ObjectManager.Player) return;
            Chat.Print("lvlup");
            Core.DelayAction(() => OnLvLUp(args.Level), RandGen.R.Next(MinTime, MaxTime));
        }

        private void OnLvLUp(int level, bool overrid=false)
        {
            if(!_enabled.CurrentValue&&!overrid)return;
            for (int z = 0; z < level; z++)
            {
                int qDesired = 0, wDesired = 0, eDesired = 0, rDesired = 0;
                for (int i = 0; i < ObjectManager.Player.Level; i++)
                {
                    switch (_skills[i])
                    {
                        case SkillToLvl.Q:
                            qDesired++;
                            break;
                        case SkillToLvl.W:
                            wDesired++;
                            break;
                        case SkillToLvl.E:
                            eDesired++;
                            break;
                        case SkillToLvl.R:
                            rDesired++;
                            break;
                    }
                }
                if (_r.Level < rDesired)
                    ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
                if (_q.Level < qDesired)
                    ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                if (_w.Level < wDesired)
                    ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                if (_e.Level < eDesired)
                    ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
            }
        }
    }
}