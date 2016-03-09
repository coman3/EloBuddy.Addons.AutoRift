using AutoRift.MyChampLogic;
using EloBuddy;
using EloBuddy.SDK;

namespace AutoRift
{
    class SkillLevelUp
    {
        private readonly IChampLogic _champ;
        private readonly SpellDataInst _q = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        private readonly SpellDataInst _w = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        private readonly SpellDataInst _e = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
        private readonly SpellDataInst _r = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);
        public SkillLevelUp(IChampLogic myChamp)
        {
            _champ = myChamp;
            Core.DelayAction(() => OnLvLUp(ObjectManager.Player.Level), RandGen.R.Next(900, 3000));
            Obj_AI_Base.OnLevelUp += Player_OnLevelUp;
        }

        void Player_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (sender != ObjectManager.Player) return;
            Core.DelayAction(()=>OnLvLUp(args.Level), RandGen.R.Next(300, 2000));
        }

        private void OnLvLUp(int level)
        {
            for (int z = 0; z < level; z++)
            {
                int qDesired = 0, wDesired = 0, eDesired = 0, rDesired = 0;
                for (int i = 0; i < ObjectManager.Player.Level; i++)
                {
                    switch (_champ.SkillSequence[i])
                    {
                        case 1:
                            qDesired++;
                            break;
                        case 2:
                            wDesired++;
                            break;
                        case 3:
                            eDesired++;
                            break;
                        case 4:
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
