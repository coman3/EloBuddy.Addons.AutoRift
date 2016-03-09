using AutoRift.MainLogics;
using EloBuddy;

namespace AutoRift.MyChampLogic
{
    internal interface IChampLogic
    {
        int[] SkillSequence { get; }
        float MaxDistanceForAa { get; }
        float OptimalMaxComboDistance { get; }
        float HarassDistance { get; }
        LogicSelector Logic { set; }
        string ShopSequence { get; }
        void Harass(AIHeroClient target);
        void Survi();
        void Combo(AIHeroClient target);
    }
}