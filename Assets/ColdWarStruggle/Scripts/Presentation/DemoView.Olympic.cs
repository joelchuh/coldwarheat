using System;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void Update()
        {
            if(!IsReady)return;
            try {var result=controller.AdvanceInternalOlympic();if(result!=null && !result.Accepted)fatal="奥运会续跑暂停："+ErrorText(result.Error);}
            catch(Exception e){fatal=e.Message;Debug.LogException(e);}
        }
        void DrawOlympicControls(PlayerViewSnapshot view)
        {
            var o=view.Olympic;
            Text(972,360,262,46,"奥运会 · 主办方："+Side(o.Sponsor),small);
            if(o.WaitingForInternal)
            {Text(972,411,262,120,"双方平手，正在继续比赛。\n由规则宿主自动掷骰，无需点击。",small);return;}
            if(o.PendingOwner!=view.Viewer)
            {
                Text(972,410,262,64,"等待 "+Side(o.PendingOwner.Value)+(o.Stage==OlympicResolutionStage.AwaitingOpponent?"选择参赛或抵制。":"选择抵制后的 OPS 用途。"),small);
                if(Button(972,480,262,40,"切换至 "+Side(o.PendingOwner.Value))){controller.SelectSide(o.PendingOwner.Value);GUIUtility.ExitGUI();}return;
            }
            if(o.Stage==OlympicResolutionStage.AwaitingOpponent)
            {
                GUI.enabled=o.CanRespond;
                if(Button(972,411,126,40,"参赛"))RespondOlympic(true);
                if(Button(1108,411,126,40,"抵制"))RespondOlympic(false);
                GUI.enabled=true;
                Text(972,467,262,98,"参赛：双方掷骰，主办方有加成；平手自动继续。\n抵制：降低 DEFCON；未触发核战时，主办方获得事件 OPS。",small);
                if(view.Practice.Defcon==2)Text(972,553,262,33,"抵制将触发核战，主办方判负。",small);return;
            }
            var options=controller.CardOptions().PendingOperations;
            Text(972,389,262,24,$"抵制已结算 · 当前 {options.CurrentOps} OPS",small);
            GUI.enabled=options.Influence==RuleError.None;if(Button(972,419,126,37,"投放影响力"))UseOlympicOperations(OperationsMode.Influence);
            GUI.enabled=options.Realignment==RuleError.None;if(Button(1108,419,126,37,"政局调整"))UseOlympicOperations(OperationsMode.Realignment);
            GUI.enabled=options.Coup==RuleError.None;if(Button(972,463,262,35,"选择政变目标")){controller.SelectCoupUse();GUIUtility.ExitGUI();}GUI.enabled=true;
            if(controller.SelectingCoup)
            {
                GUI.enabled=controller.LegalTargets().Targets.Contains(controller.SelectedCountry);
                if(Button(972,504,262,35,"确认政变："+Name(controller.SelectedCountry)))UseOlympicOperations(OperationsMode.Coup);GUI.enabled=true;
            }
            else Text(972,504,262,30,"开始一种用途后不能混用。",small);
            if(Button(972,545,262,32,"放弃这次事件 OPS"))DeclineOlympic();
        }
        string OlympicResultMessage(CardActionResult result)
        {
            var o=result.Olympic;string text="奥运会 · 主办方："+Side(o.Sponsor);var pair=o.LastPair;
            if(pair!=null)
            {
                text+=$"\n第 {pair.Round} 对：美国骰 {pair.UsRoll} → {pair.UsTotal}；苏联骰 {pair.UssrRoll} → {pair.UssrTotal}";
                text+=pair.Winner.HasValue?$"\n{Side(pair.Winner.Value)}胜出，获得 {pair.AwardVp} VP。":"\n平手，自动继续比赛。";
            }
            if(o.Branch==OlympicBranch.Boycott)text+=$"\n抵制：DEFCON {o.DefconBefore} → {o.DefconAfter}";
            if(result.Coup!=null){var c=result.Coup;text+=$"\n政变骰 {c.Roll} / OPS {c.Strength} / 差额 {c.Margin}；军事 +{c.MilitaryCredit}";}
            if(o.Outcome==OlympicPracticeOutcome.AwaitingChoice)text+="\n等待对手选择参赛或抵制。";
            if(o.Outcome==OlympicPracticeOutcome.AwaitingOperations)text+="\n等待主办方选择 OPS 用途。";
            if(o.Outcome==OlympicPracticeOutcome.OperationsCompleted)text+="\n事件 OPS 已结束，本次行动完成。";
            if(o.Outcome==OlympicPracticeOutcome.OperationsDeclined)text+="\n已放弃事件 OPS；DEFCON 下降仍保留。";
            if(o.Terminal)text+="\n对局结束："+(o.Winner.HasValue?Side(o.Winner.Value)+"获胜。":"平局。");return text;
        }
        async void RespondOlympic(bool participate){try{await controller.RespondOlympicAsync(participate);}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
        async void UseOlympicOperations(OperationsMode mode){try{await controller.ChooseOlympicOperationsAsync(mode);}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
        async void DeclineOlympic(){try{await controller.DeclineOlympicOperationsAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
