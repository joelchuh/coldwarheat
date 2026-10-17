using System;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;

namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawEventControls(PlayerViewSnapshot view)
        {
            var execution=view.Event;var query=controller.EventTargets();var draft=controller.EventDraft;
            if(execution.Stage==InfluenceEventStage.AwaitingChoice && query.Error==RuleError.None && query.Kind==InfluenceDecisionKind.Relocation)
            {DrawRelocationControls(query);return;}
            var parent=view.FiveYearPlan==null?"":"五年计划 → "+CardTitle(view.FiveYearPlan.SelectedCard.Value)+"\n";
            if(execution.Stage==InfluenceEventStage.Ready)
                Text(972,374,262,62,"这是预设的公开事件练习。\n由 "+Side(execution.PhasingPlayer)+" 开始。",small);
            else if(execution.Stage==InfluenceEventStage.Resolved)
                Text(972,374,262,62,execution.Outcome==InfluenceEventStatus.ResolvedNoChange?"事件已完成 · 无需改变影响力。":"事件已完成 · 影响力已更新。",small);
            else if(query.Error==RuleError.None && query.Kind==InfluenceDecisionKind.Allocation)
            {
                var allocations=controller.AllocationDraft;
                var amount=allocations.FirstOrDefault(a=>a.Country.Equals(controller.SelectedCountry)).Amount;
                var capacity=query.Capacities.FirstOrDefault(a=>a.Country.Equals(controller.SelectedCountry)).Amount;
                Text(972,366,262,40,$"移除草稿 {allocations.Sum(a=>a.Amount)} / {query.RequiredAmount} 点\n当前国家：{amount} / {capacity} 点",small);
                GUI.enabled=controller.CanChangeEventAllocation(controller.SelectedCountry,-1);
                if(Button(972,410,126,29,"− 1 点"))controller.ChangeEventAllocation(controller.SelectedCountry,-1);
                GUI.enabled=controller.CanChangeEventAllocation(controller.SelectedCountry,1);
                if(Button(1108,410,126,29,"+ 1 点"))controller.ChangeEventAllocation(controller.SelectedCountry,1);
                GUI.enabled=true;
            }
            else
                Text(972,366,262,75,parent+(query.Error==RuleError.WrongActor?"请切换至 "+Side(execution.Owner)+" 选择目标。":$"草稿 {draft.Count} / {query.RequiredTargets} 国\n"+string.Join("、",draft.Select(Name))),small);

            if(execution.Stage==InfluenceEventStage.Ready)
            {
                GUI.enabled=controller.SelectedSide==execution.PhasingPlayer;
                if(Button(972,445,262,39,"开始事件"))BeginEvent();
            }
            else if(execution.Stage==InfluenceEventStage.AwaitingChoice && query.Error==RuleError.WrongActor)
            {
                GUI.enabled=true;
                if(Button(972,445,262,39,"切换至 "+Side(execution.Owner))) {controller.SelectSide(execution.Owner);GUIUtility.ExitGUI();}
            }
            else
            {
                GUI.enabled=controller.CanConfirmEvent;
                if(Button(972,445,262,39,execution.Stage==InfluenceEventStage.Resolved?"已结算":query.Kind==InfluenceDecisionKind.Allocation?"确认移除分配":"确认完整选择"))ConfirmEvent();
            }
            GUI.enabled=execution.Stage==InfluenceEventStage.AwaitingChoice && draft.Count>0;
            if(Button(972,490,262,33,"清空选择草稿"))controller.ClearEventDraft();
            GUI.enabled=controller.CanRetryEvent;
            if(Button(972,531,262,33,"重发上次事件命令"))RetryEvent();
            GUI.enabled=true;
            var result=controller.LastEventResult;
            var message=ReadEventFeedback();
            var coupMessage=controller.EventPanelCoupMessage;
            DrawFeedback(574,62,coupMessage??message);
            if(coupMessage!=null && result!=null && !result.Accepted)Text(972,638,262,32,message,small);
        }
        public string ReadEventFeedback()
        {
            var result=controller.LastEventResult;
            var message=result==null?"点选只是草稿；确认后才修改局面。":InfluenceChoiceMessage(result);
            if(result?.Accepted!=false)message=ReadHeadlineFeedback()??message;
            message=controller.EventResultHistoryMessage??message;
            return message;
        }
        async void BeginEvent() { try { await controller.BeginEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void ConfirmEvent() { try { await controller.ConfirmEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void RetryEvent() { try { await controller.RetryEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
