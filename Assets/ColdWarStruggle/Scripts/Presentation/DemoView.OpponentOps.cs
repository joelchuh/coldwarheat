using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        public string ReadOperationsPreview()
        {
            var coup=controller.SelectedCoupQuote;
            if(coup!=null)return coup.Error==RuleError.None?$"政变 OPS {coup.Strength} · 军事 +{coup.MilitaryCredit}":"请选择合法政变目标。";
            var options=controller.CardOptions();var selected=controller.SelectedCard;var ordinary=options.PendingOperations?.CurrentOps??(selected.HasValue?options.Find(selected.Value)?.CurrentOps:null);
            var condition=ReadRegionalOperationsHint();return ordinary.HasValue?$"普通 OPS {ordinary}"+(condition.Length==0?"":"\n"+condition):"";
        }
        public string ReadRegionalOperationsHint()
        {
            var options=controller.CardOptions();var selected=controller.SelectedCard;var b=options.PendingOperations?.RegionalOps??(selected.HasValue?options.Find(selected.Value)?.RegionalOps:null);
            if(b==null || b.EligibleRegions.Count==0)return "";
            return "全部目标位于"+string.Join("、",b.EligibleRegions.Select(r=>names.TryGetValue(r,out var label)?label:ScoringRegionName(r)))+$"：最多 {b.Total} OPS";
        }
        void DrawOpponentOperations(PlayerViewSnapshot view)
        {
            var p=view.Practice;var options=controller.CardOptions().PendingOperations;
            Text(972,365,262,22,CardTitle(p.ActiveCard.Value),small);
            Text(972,389,262,48,ReadOperationsPreview(),small);
            GUI.enabled=options.Influence==RuleError.None;
            if(Button(972,439,126,32,"投放影响力"))UseOpponentOperations(OperationsMode.Influence);
            GUI.enabled=options.Realignment==RuleError.None;
            if(Button(1108,439,126,32,"政局调整"))UseOpponentOperations(OperationsMode.Realignment);
            GUI.enabled=options.Coup==RuleError.None;
            if(Button(972,475,262,32,"选择政变目标")){controller.SelectCoupUse();GUIUtility.ExitGUI();}
            GUI.enabled=true;
            string hint=p.OpponentOps.EventOccurred?"对手事件已结算；完成OPS后换方。":"完成OPS后仍须结算对手事件。";
            if(controller.SelectingCoup)
            {
                var targets=controller.LegalTargets();GUI.enabled=targets.Error==RuleError.None && targets.Targets.Contains(controller.SelectedCountry);
                if(Button(972,511,262,32,"确认政变："+Name(controller.SelectedCountry)))UseOpponentOperations(OperationsMode.Coup);
                GUI.enabled=true;
                if(targets.Targets.Count==0)hint="当前没有合法政变目标，可改选上方用途。";
                else if(p.Defcon==2 && view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry)).Definition.Battleground)hint="危险：战场政变触发核战，行动方判负！";
            }
            else Text(972,511,262,32,"选定并开始一种用途后，本次不能混用。",small);
            Text(972,545,262,32,hint,new GUIStyle(small){fontSize=12});
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
            if(result.ActivatedOpsEffect!=null)return message+TurnOpsEventMessage(result.ActivatedOpsEffect,result.Event);
            return message+"随后"+InfluenceEventMessage(result.Event);
        }
        async void UseOpponentOperations(OperationsMode mode)
        {try {await controller.ChooseOpponentOperationsAsync(mode);}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
