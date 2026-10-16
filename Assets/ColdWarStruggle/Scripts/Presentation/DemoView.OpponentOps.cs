using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawOpponentOperations(PlayerViewSnapshot view)
        {
            var p=view.Practice;var options=controller.CardOptions().PendingOperations;
            Text(972,365,262,44,CardTitle(p.ActiveCard.Value)+$"\n现在选择 OPS 用途 · {options.CurrentOps} 点",small);
            GUI.enabled=options.Influence==RuleError.None;
            if(Button(972,411,126,38,"投放影响力"))UseOpponentOperations(OperationsMode.Influence);
            GUI.enabled=options.Realignment==RuleError.None;
            if(Button(1108,411,126,38,"政局调整"))UseOpponentOperations(OperationsMode.Realignment);
            GUI.enabled=options.Coup==RuleError.None;
            if(Button(972,457,262,36,"选择政变目标")){controller.SelectCoupUse();GUIUtility.ExitGUI();}
            GUI.enabled=true;
            string hint=p.OpponentOps.EventOccurred?"对手事件已结算；完成OPS后换方。":"完成OPS后仍须结算对手事件。";
            if(controller.SelectingCoup)
            {
                var targets=controller.LegalTargets();GUI.enabled=targets.Error==RuleError.None && targets.Targets.Contains(controller.SelectedCountry);
                if(Button(972,500,262,36,"确认政变："+Name(controller.SelectedCountry)))UseOpponentOperations(OperationsMode.Coup);
                GUI.enabled=true;
                if(targets.Targets.Count==0)hint="当前没有合法政变目标，可改选上方用途。";
                else if(p.Defcon==2 && view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry)).Definition.Battleground)hint="危险：战场政变触发核战，行动方判负！";
            }
            else Text(972,500,262,32,"选定并开始一种用途后，本次不能混用。",small);
            Text(972,538,262,38,hint,small);
        }
        string CombinedCoupMessage(CardActionResult result)
        {
            var c=result.Coup;
            var message=$"政变骰 {c.Roll} / OPS {c.Strength} / 差额 {c.Margin}\n影响力 −{c.Removed}/+{c.Added} · 军事 +{c.MilitaryCredit}\n";
            if(result.War!=null)return message+WarResultMessage(result.War);
            if(result.FiveYearPlan!=null)
            {
                var f=result.FiveYearPlan;var name=f.SelectedCard.HasValue?CardTitle(f.SelectedCard.Value):"无手牌";
                var progress=f.Outcome==FiveYearPlanOutcome.AwaitingUsEvent?"等待美国":f.Outcome==FiveYearPlanOutcome.Discarded?"弃置":f.Outcome==FiveYearPlanOutcome.Interrupted?"终局":"完成";
                return message+"五年计划 → "+name+" · "+progress+FiveYearPlanChildDetail(result);
            }
            if(result.Blockade!=null)return message+(result.Blockade.Outcome==BlockadeOutcome.AwaitingChoice
                ?"封锁：等待"+(result.Blockade.Owner==PlayerSide.US?"美国":"苏联")+"回应。"
                :"封锁："+Name(result.Blockade.FallbackCountry)+(result.Blockade.Owner==PlayerSide.US?"美方":"苏方")+"影响力清零。");
            if(result.DefconVpEvent!=null)
            {
                var e=result.DefconVpEvent;return message+$"事件 DEFCON {e.DefconFrom}→{e.DefconTo}"+(e.Terminal?" · 终局":" · VP "+e.VpForUS.ToString("+0;-0;0"));
            }
            if(result.ActivatedOpsEffect!=null)return message+"持续OPS已生效："+Side(result.ActivatedOpsEffect.AffectedSide);
            return message+(result.Event.Stage==InfluenceEventStage.AwaitingChoice?"随后事件：等待受益方选择。":"随后事件：已完成。");
        }
        async void UseOpponentOperations(OperationsMode mode)
        {try {await controller.ChooseOpponentOperationsAsync(mode);}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
