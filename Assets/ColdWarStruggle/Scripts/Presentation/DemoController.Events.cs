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
            get { var q=EventTargets();return q.Error==RuleError.None && q.RequiredTargets>0 && eventDraft.Count==q.RequiredTargets; }
        }
        public IReadOnlyList<CountryId> EventDraft
        { get { EnsureDraftContext(ReadView());return Array.AsReadOnly(eventDraft.ToArray()); } }
        void EnsureDraftContext(PlayerViewSnapshot view)
        {
            if(draftGame==view.GameId && draftVersion==view.Version && draftSide==SelectedSide && draftDecision==view.Event?.DecisionId)return;
            eventDraft.Clear();eventTargets=null;draftGame=view.GameId;draftVersion=view.Version;draftSide=SelectedSide;draftDecision=view.Event?.DecisionId;
        }
        void ResetEventInteraction(bool clearReceipt)
        {
            eventDraft.Clear();eventTargets=null;draftGame=null;draftVersion=-1;draftDecision=null;LastEventResult=null;
            if(clearReceipt)lastEventCommand=null;
        }
        public EventTargetsResult EventTargets()
        {
            var view=ReadView();EnsureDraftContext(view);
            return eventTargets??(eventTargets=Session.QueryEventTargets(view.Event?.ExecutionId,view.Version));
        }
        public bool ToggleEventTarget(CountryId country)
        {
            var query=EventTargets();if(query.Error!=RuleError.None || !query.Targets.Contains(country))return false;
            if(eventDraft.Remove(country))return true;
            if(eventDraft.Count>=query.RequiredTargets)return false;
            eventDraft.Add(country);return true;
        }
        public void ClearEventDraft() { eventDraft.Clear(); }
        public Task<EventCommandResult> BeginEventAsync()=>SubmitEvent(EventCommandKind.Begin);
        public Task<EventCommandResult> ConfirmEventAsync()=>SubmitEvent(EventCommandKind.Resolve);
        async Task<EventCommandResult> SubmitEvent(EventCommandKind kind)
        {
            var view=ReadView();EnsureDraftContext(view);
            var command=new EventCommandEnvelope(Guid.NewGuid().ToString("N"),view.GameId,view.Version,kind,view.Event?.ExecutionId,
                kind==EventCommandKind.Resolve?view.Event?.DecisionId:null,kind==EventCommandKind.Resolve?eventDraft:null);
            var epoch=generation;lastEventCommand=command;lastEventSide=SelectedSide;
            var result=await Session.SubmitEventAsync(command);
            if(epoch==generation)
            {
                LastEventResult=result;legal=null;eventTargets=null;
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
