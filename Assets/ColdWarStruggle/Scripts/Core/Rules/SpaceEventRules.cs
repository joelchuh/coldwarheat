using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static class SpaceEventRules
    {
        static void ValidateBinding(SpaceAttemptState state,SpaceEventRequest request)
        {
            if(state==null || request==null)throw new ArgumentNullException();
            if(request.TurnNumber!=state.Turn.Number || !ReferenceEquals(state.Track.Definition,request.Definition.Track))
                throw new ArgumentException("Space event belongs to a different turn or track.");
        }
        // This is arithmetic eligibility, not player authorization or a duplicate-command check.
        public static SpaceEventEligibility QueryEligibility(SpaceAttemptState state,SpaceEventRequest request)
        {
            ValidateBinding(state,request);
            return state.Track.PositionFor(request.Beneficiary)==state.Track.Definition.FinalPosition
                ?SpaceEventEligibility.TerminalRequiresRuling:SpaceEventEligibility.Allowed;
        }
        public static SpaceEventPlan PlanAdvance(SpaceAttemptState state,SpaceEventRequest request)
        {
            ValidateBinding(state,request);
            if(state.Receipts.ContainsKey(request.Id))throw new ArgumentException("Space ID already belongs to an ordinary attempt.");
            if(state.EventReceipts.TryGetValue(request.Id,out var prior))
            {
                if(!prior.Request.SameFacts(request))throw new ArgumentException("Conflicting space event ID.");
                return new SpaceEventPlan(state,state,prior,true);
            }
            if(QueryEligibility(state,request)!=SpaceEventEligibility.Allowed)throw new NotSupportedException("SPACE_EVENT_TERMINAL_REQUIRES_RULING");
            if(state.ReceiptCount>=SpaceAttemptState.MaximumReceipts)throw new InvalidOperationException("Space receipt engineering limit reached.");
            var advance=SpaceAdvanceRules.CalculateSingleStep(state.Track,request.Beneficiary);
            var receipt=new SpaceEventReceipt(request,advance);
            var receipts=state.EventReceipts.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            receipts.Add(request.Id,receipt);
            var after=new SpaceAttemptState(advance.After,state.Calendar,state.Turn,state.UsAttempts,state.UssrAttempts,state.Receipts,receipts);
            return new SpaceEventPlan(state,after,receipt,false);
        }
    }
}
