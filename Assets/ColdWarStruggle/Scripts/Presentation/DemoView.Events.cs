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
                Text(972,366,262,75,query.Error==RuleError.WrongActor?"请切换至 "+Side(execution.Owner)+" 选择目标。":$"草稿 {draft.Count} / {query.RequiredTargets} 国\n"+string.Join("、",draft.Select(Name)),small);

            if(execution.Stage==InfluenceEventStage.Ready)
            {
                GUI.enabled=controller.SelectedSide==execution.PhasingPlayer;
                if(Button(972,445,262,39,"开始事件"))BeginEvent();
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
            var message=result==null?"点选只是草稿；确认后才修改局面。"
                :!result.Accepted?"未执行："+ErrorText(result.Error)
                :result.ResolutionSequence.HasValue?"完成收据 · 变化 "+result.Changes.Count+" 项\n序号 "+result.ResolutionSequence.Value
                :"选择请求已建立。请按左侧提示操作。";
            Text(972,574,262,62,message,small);
        }
        async void BeginEvent() { try { await controller.BeginEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void ConfirmEvent() { try { await controller.ConfirmEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void RetryEvent() { try { await controller.RetryEventAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
