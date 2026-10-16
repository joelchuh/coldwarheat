using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;

namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public EventCommandResult LastEventResult { get; private set; }
        public string EventResultHistoryMessage
        {
            get
            {
                var receipt=LastEventResult?.View;if(receipt==null)return null;var current=ReadView();
                return receipt.GameId==current.GameId && receipt.Version==current.Version?null
                    :$"历史事件结果（当时 v{receipt.Version}）\n当前 v{current.Version}；重发不会重复结算。";
            }
        }
        readonly List<CountryId> eventDraft=new List<CountryId>();
        EventCommandEnvelope lastEventCommand;
        PlayerSide lastEventSide;
        EventTargetsResult eventTargets;
        string draftGame,draftDecision;
        long draftVersion=-1;
        PlayerSide draftSide;
        public bool CanRetryEvent=>lastEventCommand!=null && lastEventSide==SelectedSide;
        public bool CanConfirmEvent
        {
            get
            {
                var q=EventTargets();if(q.Error!=RuleError.None)return false;
                return q.Kind==InfluenceDecisionKind.Allocation?q.RequiredAmount>0 && allocationDraft.Values.Sum(v=>(long)v)==q.RequiredAmount
                    :q.Kind==InfluenceDecisionKind.CountrySet && q.RequiredTargets>0 && eventDraft.Count==q.RequiredTargets;
            }
        }
        public IReadOnlyList<CountryId> EventDraft
        { get { EnsureDraftContext(ReadView());return Array.AsReadOnly(eventDraft.Concat(allocationDraft.Keys.OrderBy(c=>c.Value,StringComparer.Ordinal)).ToArray()); } }
        void EnsureDraftContext(PlayerViewSnapshot view)
        {
            if(draftGame==view.GameId && draftVersion==view.Version && draftSide==SelectedSide && draftDecision==view.Event?.DecisionId)return;
            eventDraft.Clear();allocationDraft.Clear();eventTargets=null;draftGame=view.GameId;draftVersion=view.Version;draftSide=SelectedSide;draftDecision=view.Event?.DecisionId;MarkInteractionChanged();
        }
        void ResetEventInteraction(bool clearReceipt)
        {
            eventDraft.Clear();allocationDraft.Clear();eventTargets=null;draftGame=null;draftVersion=-1;draftDecision=null;LastEventResult=null;MarkInteractionChanged();
            if(clearReceipt)lastEventCommand=null;
        }
        public EventTargetsResult EventTargets()
        {
            var view=ReadView();EnsureDraftContext(view);
            return eventTargets??(eventTargets=Session.QueryEventTargets(view.Event?.ExecutionId,view.Version));
        }
        public bool ToggleEventTarget(CountryId country)
        {
            var query=EventTargets();if(query.Error!=RuleError.None || query.Kind!=InfluenceDecisionKind.CountrySet || !query.Targets.Contains(country))return false;
            if(eventDraft.Remove(country)) { MarkInteractionChanged();return true; }
            if(eventDraft.Count>=query.RequiredTargets)return false;
            eventDraft.Add(country);MarkInteractionChanged();return true;
        }
        public void ClearEventDraft()
        {
            EnsureDraftContext(ReadView());
            if(eventDraft.Count==0 && allocationDraft.Count==0)return;
            eventDraft.Clear();allocationDraft.Clear();MarkInteractionChanged();
        }
        public Task<EventCommandResult> BeginEventAsync()=>SubmitEvent(EventCommandKind.Begin);
        public Task<EventCommandResult> ConfirmEventAsync()=>SubmitEvent(EventTargets().Kind==InfluenceDecisionKind.Allocation?EventCommandKind.ResolveAllocation:EventCommandKind.Resolve);
        async Task<EventCommandResult> SubmitEvent(EventCommandKind kind)
        {
            var view=ReadView();EnsureDraftContext(view);
            var id=Guid.NewGuid().ToString("N");
            var command=kind==EventCommandKind.ResolveAllocation?
                EventCommandEnvelope.Allocate(id,view.GameId,view.Version,view.Event?.ExecutionId,view.Event?.DecisionId,AllocationDraft)
                :new EventCommandEnvelope(id,view.GameId,view.Version,kind,view.Event?.ExecutionId,
                kind==EventCommandKind.Resolve?view.Event?.DecisionId:null,kind==EventCommandKind.Resolve?eventDraft:null);
            var epoch=generation;lastEventCommand=command;lastEventSide=SelectedSide;
            var result=await Session.SubmitEventAsync(command);
            if(epoch==generation)
            {
                LastEventResult=result;legal=null;eventTargets=null;
                if(result.Accepted){selectedCard=null;SelectingCoup=false;}
                EnsureDraftContext(ReadView());
            }
            return result;
        }
        public async Task<EventCommandResult> RetryEventAsync()
        {
            if(!CanRetryEvent)throw new InvalidOperationException("No event command for this side.");
            var epoch=generation;var result=await Session.SubmitEventAsync(lastEventCommand);
            if(epoch==generation)LastEventResult=result;
            return result;
        }
    }
}
