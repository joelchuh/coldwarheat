using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        string SpaceEventResultMessage(PublicSpaceEventResult e)
            =>"太空事件 · "+CardTitle(e.Card)+"\n"+Side(e.Beneficiary)+$"：{e.FromPosition} → {e.ToPosition} · "+(e.FirstArrival?"先到":"后到")+$" +{e.AwardVp} VP\n"
                +"不掷骰 · 不消耗普通太空次数\n"+SpaceAbilityMessage(e.BeforeUS,e.AfterUS)+"\n"+SpaceAbilityMessage(e.BeforeUSSR,e.AfterUSSR);
        static string SpaceAbilityMessage(SpaceAbilitySnapshot before,SpaceAbilitySnapshot after)
        {
            var text=Side(after.Owner)+$"：尝试上限 {before.AttemptLimit}→{after.AttemptLimit}";
            if(before.OpponentRevealsHeadlineFirst!=after.OpponentRevealsHeadlineFirst)text+=after.OpponentRevealsHeadlineFirst?"；获得头条后选":"；失去头条后选";
            if(before.EndTurnDiscardMaximum!=after.EndTurnDiscardMaximum)text+=$"；期末弃牌 {before.EndTurnDiscardMaximum}→{after.EndTurnDiscardMaximum}";
            if(before.ActionRoundAllowance!=after.ActionRoundAllowance)text+="；行动上限 "+(before.ActionRoundAllowance?.ToString()??"普通")+"→"+(after.ActionRoundAllowance?.ToString()??"普通");
            return text;
        }
    }
}
