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
            Text(28,70,1220,30,$"{Side(p.Actor)} 行动 {p.Round}/{p.RoundLimit}  |  VP（美+ / 苏−）{p.VpForUS}  |  DEFCON {p.Defcon?.ToString()??"—"}  |  军事累计 美 {p.UsMilitary?.ToString()??"—"} / 苏 {p.UssrMilitary?.ToString()??"—"}  |  太空 美 {p.UsSpace} / 苏 {p.UssrSpace}",small);
        }
        void DrawPracticeControls(PlayerViewSnapshot view)
        {
            var p=view.Practice;
            if(p.Winner.HasValue)
                Text(972,374,262,140,"核战结束 · "+Side(p.Winner.Value)+" 获胜。\n导致 DEFCON 降至 1 的行动方判负。\n可重置后尝试其他路线。",label);
            else if(p.Stage==ActionPracticeStage.Complete)
                Text(972,374,262,140,"本回合行动试玩已结束。\n回合末审核与下一回合尚未接入。可重置重新尝试不同路线。",label);
            else if(p.Actor!=view.Viewer)
            {
                Text(972,374,262,66,"行动已交给 "+Side(p.Actor)+"。\n当前操作方不能替对方出牌。",small);
                if(Button(972,445,262,42,"切换至 "+Side(p.Actor))) {controller.SelectSide(p.Actor);GUIUtility.ExitGUI();}
            }
            else if(p.Stage==ActionPracticeStage.PlacingInfluence)
            {
                Text(972,368,262,38,"使用："+CardTitle(p.ActiveCard.Value),small);
                GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                if(Button(972,410,262,39,"投放 1 点影响力"))Place(1);
                GUI.enabled=true;
                if(Button(972,457,262,39,view.RemainingOps==0?"结束行动并弃牌":$"结束行动（放弃 {view.RemainingOps} OPS）"))FinishCard();
                Text(972,504,262,36,"点地图选国，再投放；绿色为合法目标。",small);
            }
            else
            {
                var selected=controller.SelectedCard;
                if(!selected.HasValue)
                {
                    Text(972,374,262,59,"先从本方手牌选择一张牌，再决定用于影响力、政变或太空。",small);
                    if(Button(972,440,262,43,"打开手牌 · 选择一张"))OpenCards();
                }
                else
                {
                    var option=controller.CardOptions().Cards.First(c=>c.Card.Equals(selected.Value));
                    Text(972,365,262,44,CardTitle(selected.Value),small);
                    GUI.enabled=option.Influence==RuleError.None;
                    if(Button(972,411,126,40,"用于影响力"))UseCard(CardActionKind.Influence);
                    GUI.enabled=option.Space==RuleError.None;
                    if(Button(1108,411,126,40,"送入太空"))UseCard(CardActionKind.Space);
                    GUI.enabled=true;
                    GUI.enabled=option.Coup==RuleError.None;
                    if(Button(972,458,126,35,"选择政变目标")) {controller.SelectCoupUse();GUIUtility.ExitGUI();}
                    GUI.enabled=true;
                    if(Button(1108,458,126,35,"重新选牌"))OpenCards();
                    if(controller.SelectingCoup)
                    {
                        GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                        if(Button(972,500,262,36,"确认政变："+Name(controller.SelectedCountry)))UseCard(CardActionKind.Coup);
                        GUI.enabled=true;
                        var country=view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry));
                        if(p.Defcon==2 && country.Definition.Battleground)Text(972,538,262,36,"危险：战场政变会触发核战，当前行动方判负！",small);
                    }
                    var reason=option.Influence!=RuleError.None?"影响力："+ErrorText(option.Influence):"";
                    if(option.Space!=RuleError.None)reason+="\n太空："+ErrorText(option.Space);
                    if(!controller.SelectingCoup)Text(972,500,262,58,reason,small);
                }
            }
            var result=controller.LastCardResult;
            string message="";
            if(result?.Coup!=null)
            {
                var c=result.Coup;message=$"政变骰 {c.Roll} + OPS {c.Strength}，差额 {c.Margin}\n移除 {c.Removed} / 增加 {c.Added} · 军事 +{c.MilitaryCredit}";
                if(c.DefconTo.HasValue)message+=$"\nDEFCON {c.DefconFrom} → {c.DefconTo}";
            }
            else if(result!=null)message=!result.Accepted?ErrorText(result.Error):result.Space!=null
                ?$"太空骰 {result.Space.Roll} · {(result.Space.Success?"成功":"失败")}，轨道 {result.Space.From} → {result.Space.To}\nVP 变化 {result.Space.VpForUS:+0;-0;0}；牌已弃置。":"行动命令已接受。";
            if(controller.LastResult!=null)message=controller.LastResult.Accepted?"投放成功，费用："+string.Join(" + ",controller.LastResult.Costs):ErrorText(controller.LastResult.Error);
            Text(972,578,262,65,message,small);
            GUI.enabled=controller.CanRetryCard;
            if(Button(972,646,262,27,"重发出牌命令（幂等）"))RetryCard();
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
        async void RetryCard()
        { try { await controller.RetryCardAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
