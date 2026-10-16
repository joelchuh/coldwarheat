using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        string CardTitle(CardId id)=>names.TryGetValue(id.Value,out var text)?text:id.Value;
        void OpenCards() { handPanelOpen=true;handScroll=Vector2.zero; }
        void DrawPracticeStatus(PlayerViewSnapshot view)
        {
            var p=view.Practice;
            Block(new Rect(26,67,1228,38),Background);
            Text(28,70,1220,30,$"回合 {view.Turn.Number} · "+controller.PracticePhaseText+$"  |  VP（美+ / 苏−）{p.VpForUS}  |  DEFCON {p.Defcon?.ToString()??"—"}  |  军事累计 美 {p.UsMilitary?.ToString()??"—"} / 苏 {p.UssrMilitary?.ToString()??"—"}  |  太空 美 {p.UsSpace} / 苏 {p.UssrSpace}"+(view.Event?.Stage==InfluenceEventStage.AwaitingChoice?"  |  事件选择："+Side(view.Event.Owner):""),small);
        }
        string OpsEffectSummary(PublicTurnOpsEffect effect)=>CardTitle(effect.Card)+" → "+Side(effect.AffectedSide)+$" {effect.Delta:+0;-0;0}";
        void DrawOpsEffects(PublicPracticeView practice)
        {
            var text=practice.OpsEffects.Count==0?"本回合持续 OPS：无":"本回合持续 OPS："+string.Join("；",practice.OpsEffects.Select(OpsEffectSummary))+"  ·  回合末到期";
            var hasChoice=practice.IsExtraAction || practice.CanSkipEmptyHand;
            if(hasChoice)text=$"已用名额 美 {practice.UsCompletedActions} / 苏 {practice.UssrCompletedActions} · "+text;
            Text(28,155,hasChoice?920:1220,22,text,new GUIStyle(small){fontSize=12});
        }
        void DrawPracticeControls(PlayerViewSnapshot view)
        {
            var p=view.Practice;
            if(p.Stage==ActionPracticeStage.CollectingHeadline){DrawHeadlineControls(view);return;}
            if(p.TurnEnd!=null) { DrawTurnEndReport(view);return; }
            if(p.Winner.HasValue)
                Text(972,374,262,140,VictoryMessage(p),label);
            else if(p.Stage==ActionPracticeStage.Complete)
            {
                Text(972,374,262,100,controller.TurnEndPrompt,small);
                GUI.enabled=p.TurnEndAvailable;
                if(Button(972,485,262,42,"结算本回合"))AssessTurnEnd();
                GUI.enabled=true;
            }
            else if(p.Actor!=view.Viewer)
            {
                Text(972,374,262,66,"行动已交给 "+Side(p.Actor)+"。\n当前操作方不能替对方出牌。",small);
                if(Button(972,445,262,42,"切换至 "+Side(p.Actor))) {controller.SelectSide(p.Actor);GUIUtility.ExitGUI();}
            }
            else if(p.Stage==ActionPracticeStage.ChoosingOperations)DrawOpponentOperations(view);
            else if(p.Stage==ActionPracticeStage.Realigning)
            {
                Text(972,360,262,46,"调整："+CardTitle(p.ActiveCard.Value)+"\n"+RemainingOpsText(view),small);
                GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                if(Button(972,410,262,39,"调整一次："+Name(controller.SelectedCountry)))RollRealignment();
                GUI.enabled=true;
                if(Button(972,457,262,39,view.RemainingOps==0?"结束调整":$"结束调整（放弃{(view.RegionalOps?.EligibleRegions.Count>0?"最多":"")} {view.RemainingOps} OPS）"))FinishRealignment();
                Text(972,504,262,66,(view.RegionalOps!=null?RegionalBudgetHint(view):$"剩余 {view.RemainingOps} OPS；每次后可重选国家。")+"\n"+(p.OpponentOps!=null && !p.OpponentOps.EventOccurred?"结束调整后仍须结算对手事件。":"双方掷骰，自己也可能失去影响力。"),small);
            }
            else if(p.Stage==ActionPracticeStage.PlacingInfluence)
            {
                Text(972,360,262,46,"使用："+CardTitle(p.ActiveCard.Value)+"\n"+RemainingOpsText(view),small);
                GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                if(Button(972,410,262,39,"投放 1 点影响力"))Place(1);
                GUI.enabled=true;
                if(Button(972,457,262,39,view.RemainingOps==0?"结束投放":$"结束投放（放弃{(view.RegionalOps?.EligibleRegions.Count>0?"最多":"")} {view.RemainingOps} OPS）"))FinishCard();
                Text(972,504,262,66,p.OpponentOps==null?(view.RegionalOps!=null?RegionalBudgetHint(view):"点地图选国，再投放；绿色为合法目标。"):p.OpponentOps.EventOccurred?"对手事件已完成。结束投放后交给下一方。":"结束投放后仍须结算对手事件；现在不会换方。",small);
            }
            else
            {
                var selected=controller.SelectedCard;
                if(!selected.HasValue)
                {
                    Text(972,374,262,59,controller.CanSkipEmptyHand?"普通手牌已用尽。可用上方按钮跳过本次行动；中国牌仍可自愿选择。":"先从本方手牌选择一张牌，再决定用于影响力、政变、调整、太空、计分或已支持事件。",small);
                    if(Button(972,440,262,43,"打开手牌 · 选择一张"))OpenCards();
                }
                else
                {
                    var option=controller.CardOptions().Find(selected.Value);
                    var coupQuote=controller.SelectedCoupQuote;
                    var opsText=coupQuote?.Error==RuleError.None?$"\n政变 OPS {coupQuote.Strength} · 军事 +{coupQuote.MilitaryCredit}":option.CurrentOps.HasValue?$"\n印刷 OPS {option.PrintedOps} → 普通 OPS {option.CurrentOps}":"";
                    Text(972,365,262,44,CardTitle(selected.Value)+opsText,small);
                    if(option.Scoring==RuleError.None)DrawScoringChoice(selected.Value);
                    else
                    {
                    var opponent=option.OpponentEventFirst==RuleError.None;
                    GUI.enabled=opponent || option.Influence==RuleError.None;
                    if(Button(972,411,126,40,opponent?"先结算事件":"用于影响力"))UseCard(opponent?CardActionKind.OpponentEventFirst:CardActionKind.Influence);
                    GUI.enabled=option.Space==RuleError.None;
                    if(Button(1108,411,126,40,"送入太空"))UseCard(CardActionKind.Space);
                    GUI.enabled=true;
                    GUI.enabled=opponent || option.Coup==RuleError.None;
                    if(Button(972,458,126,35,opponent?"先使用 OPS":"选择政变目标"))
                    { if(opponent)UseCard(CardActionKind.OpponentOpsFirst);else {controller.SelectCoupUse();GUIUtility.ExitGUI();} }
                    GUI.enabled=true;
                    GUI.enabled=option.Event==RuleError.None;
                    if(Button(1108,458,126,35,option.Event==RuleError.None?"发动事件":selected.Value.Equals(view.Cards.China.Id)?"中国牌无事件":"事件未接入"))UseCard(CardActionKind.Event);
                    GUI.enabled=true;
                    if(controller.SelectingCoup)
                    {
                        GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                        if(Button(972,500,262,36,"确认政变："+Name(controller.SelectedCountry)))UseCard(CardActionKind.Coup);
                        GUI.enabled=true;
                        var country=view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry));
                        if(p.Defcon==2 && country.Definition.Battleground)Text(972,538,262,36,"危险：战场政变会触发核战，当前行动方判负！",small);
                    }
                    var reason=opponent?"对手牌：先选顺序，到OPS步骤再选用途。":option.Influence!=RuleError.None?"影响力："+ErrorText(option.Influence):"";
                    if(option.RegionalMaximum.HasValue && option.Influence==RuleError.None)reason=$"全用于亚洲最多 {option.RegionalMaximum} OPS；太空仍 {option.CurrentOps}。";
                    if(option.Space!=RuleError.None)reason+=(reason.Length==0?"":"\n")+"太空："+ErrorText(option.Space);
                    if(!controller.SelectingCoup)
                    {
                        GUI.enabled=option.Realignment==RuleError.None;
                        if(Button(972,500,126,32,"政局调整"))UseCard(CardActionKind.Realignment);
                        GUI.enabled=true;
                        if(Button(1108,500,126,32,"重新选牌"))OpenCards();Text(972,537,262,38,reason,small);
                    }
                    }
                }
            }
            var result=controller.LastCardResult;
            string message="";
            if(result?.Realignment!=null)
            {
                var r=result.Realignment;
                message=$"美国 {r.UsRoll} + {r.US.Total} = {r.UsTotal}；苏联 {r.UssrRoll} + {r.USSR.Total} = {r.UssrTotal}\n";
                message+=r.RemovedSide.HasValue?$"{Name(r.Target)}：{Side(r.RemovedSide.Value)}移除 {r.Removed}\n可重选目标，或结束调整。":$"{Name(r.Target)}：影响力无变化。\n本次仍消耗 OPS。";
            }
            else if(result?.ActivatedOpsEffect!=null)message="持续效果已生效\n"+OpsEffectSummary(result.ActivatedOpsEffect)+"\n本回合有效，弃牌或移出不取消。";
            else if(result?.DefconVpEvent!=null)
            {
                var e=result.DefconVpEvent;
                message=string.Join("\n",e.Steps.Select(step=>step switch {
                    DefconVpStep.DefconChanged=>$"DEFCON {e.DefconFrom} → {e.DefconTo}",
                    DefconVpStep.VpBatchClosed=>$"{Side(e.AwardSide)}获得 {e.Award} VP",
                    _=>"核战 · 后续步骤停止" }));
                if(e.Terminal && !e.Steps.Contains(DefconVpStep.NuclearWarReached))message+="\n已达胜利条件 · 后续步骤停止";
            }
            else if(result?.Event!=null)message=result.Event.Stage==InfluenceEventStage.AwaitingChoice?"等待事件受益方完成选择。":result.Event.Outcome==InfluenceEventStatus.ResolvedNoChange?"事件完成 · 影响力无需变化。":"事件完成 · 影响力变化 "+result.Event.Changes.Count+" 项。";
            else if(result?.Scoring!=null)message="计分结果："+ScoringRegionName(result.Scoring.RegionId)+"\n"+ScoreSummary(result.Scoring);
            else if(result?.Coup!=null)
            {
                var c=result.Coup;message=$"政变骰 {c.Roll} + OPS {c.Strength}，差额 {c.Margin}\n移除 {c.Removed} / 增加 {c.Added} · 军事 +{c.MilitaryCredit}";
                if(c.DefconTo.HasValue)message+=$"\nDEFCON {c.DefconFrom} → {c.DefconTo}";
            }
            else if(result!=null)message=!result.Accepted?ErrorText(result.Error):result.Space!=null
                ?$"太空骰 {result.Space.Roll} · {(result.Space.Success?"成功":"失败")}，轨道 {result.Space.From} → {result.Space.To}\nVP 变化 {result.Space.VpForUS:+0;-0;0}；{(result.View.Practice.Winner.HasValue?"终局停止收尾":"行动结束，请查看牌区")}。":"行动命令已接受。";
            if(result?.Coup!=null && (result.Event!=null || result.DefconVpEvent!=null || result.ActivatedOpsEffect!=null))message=CombinedCoupMessage(result);
            if(controller.LastResult!=null)message=controller.LastResult.Accepted?"投放成功，费用："+string.Join(" + ",controller.LastResult.Costs):ErrorText(controller.LastResult.Error);
            if(controller.LastEventResult!=null)message=controller.LastEventResult.Accepted?"事件选择完成 · 影响力变化 "+controller.LastEventResult.Changes.Count+" 项。":ErrorText(controller.LastEventResult.Error);
            DrawExtraActionChoice(view);
            DrawEmptyHandChoice(view);
            Text(972,578,262,65,message,small);
            var retryHeadline=!controller.CanRetryCard && controller.CanRetryHeadline;
            GUI.enabled=controller.CanRetryCard || retryHeadline;
            if(Button(972,646,262,27,retryHeadline?"重发头条命令（幂等）":"重发出牌命令（幂等）"))
            {if(retryHeadline)RetryHeadline();else RetryCard();}
            GUI.enabled=true;
        }
        async void StartCardPractice()
        {
            try { controller.SelectSide(PlayerSide.USSR);await controller.StartPracticeAsync(); }
            catch(Exception e) { fatal=e.Message;Debug.LogException(e); }
        }
        async void UseCard(CardActionKind kind)
        { try { await controller.PlayCardAsync(kind); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void FinishCard()
        { try { await controller.FinishInfluenceAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void RollRealignment()
        { try { await controller.RollRealignmentAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void FinishRealignment()
        { try { await controller.FinishRealignmentAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void RetryCard()
        { try { await controller.RetryCardAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
