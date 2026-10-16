using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static class OlympicResolutionRules
    {
        public static OlympicResolution Begin(OlympicEventState state,CardZones cards,string gameId,string resolutionId,long revision,OlympicSourceKind kind,PlayerSide sponsor,PlayerSide rootPhasing)
        {
            if(state==null || cards==null)throw new ArgumentNullException();
            VictoryGuard.Text(gameId);VictoryGuard.Text(resolutionId);
            OlympicContestRules.RequireSide(sponsor);OlympicContestRules.RequireSide(rootPhasing);
            if(revision<0 || kind!=OlympicSourceKind.ActionRound && kind!=OlympicSourceKind.Headline || rootPhasing!=sponsor
                || !ReferenceEquals(state.Definition.Cards,cards.Catalog) || cards.ResolvingCards.Count!=1
                || !cards.ResolvingCards.Contains(state.Definition.Card.Id) || state.Defcon.Level==state.Defcon.Definition.NuclearLevel)
                throw new ArgumentException("Unsupported Olympic root, card partition or terminal DEFCON.");
            var live=VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,state.Definition.EffectId,state.VpForUS,0,0));
            return new OlympicResolution(state,cards,gameId,resolutionId,revision,kind,sponsor,rootPhasing,OlympicResolutionStage.AwaitingOpponent,decision:live);
        }
        public static OlympicResolution Respond(OlympicResolution state,PlayerSide actor,string decisionId,long expectedRevision,OlympicBranch branch,IRandomProvider random)
        {
            RequireStage(state,OlympicResolutionStage.AwaitingOpponent);
            RequireReply(state,actor,expectedRevision);
            if(decisionId!=state.DecisionId || branch!=OlympicBranch.Participate && branch!=OlympicBranch.Boycott)
                throw new ArgumentException("Unknown or stale Olympic choice.");
            var revision=checked(state.Revision+1);
            if(branch==OlympicBranch.Participate)return Pair(state,revision,random);
            var boycott=OlympicEventService.PlanBoycott(state.Current,state.Sponsor,state.RootPhasing,state.ResolutionId,OlympicInterferencePolicy.NoActiveInterference);
            return Copy(state,boycott.After,revision,boycott.Decision.Outcome==VictoryOutcome.Finished?OlympicResolutionStage.Completed:OlympicResolutionStage.AwaitingOperations,
                branch,boycott:boycott,decision:boycott.Decision);
        }
        public static OlympicResolution ContinueOne(OlympicResolution state,OlympicContinuation continuation,IRandomProvider random)
        {
            RequireStage(state,OlympicResolutionStage.Rolling);
            if(continuation==null || !ReferenceEquals(state.Continuation,continuation) || !ReferenceEquals(continuation.Owner,state))
                throw new ArgumentException("Current Olympic continuation token required.");
            return Pair(state,checked(state.Revision+1),random);
        }
        public static OlympicResolution DeclineOperations(OlympicResolution state,PlayerSide actor,long expectedRevision)
        {
            RequireStage(state,OlympicResolutionStage.AwaitingOperations);RequireReply(state,actor,expectedRevision);
            return Copy(state,state.Current,checked(state.Revision+1),OlympicResolutionStage.Completed,OlympicBranch.Boycott,boycott:state.Boycott,decision:state.Decision);
        }
        static OlympicResolution Pair(OlympicResolution state,long revision,IRandomProvider random)
        {
            // Preflight bookkeeping overflow before the independent random fork.
            _=checked((state.LastPair?.RoundIndex??0)+1);
            var pair=OlympicEventService.PlanPair(state.Current,state.Sponsor,state.ResolutionId,OlympicInterferencePolicy.NoActiveInterference,random);
            var receipt=new OlympicPairReceipt(state.LastPair,revision,pair);
            return Copy(state,pair.After,revision,pair.RequiresAnotherPair?OlympicResolutionStage.Rolling:OlympicResolutionStage.Completed,
                OlympicBranch.Participate,last:receipt,decision:pair.Decision);
        }
        static OlympicResolution Copy(OlympicResolution source,OlympicEventState current,long revision,OlympicResolutionStage stage,
            OlympicBranch branch,OlympicPairReceipt last=null,OlympicBoycottPlan boycott=null,VictoryDecision decision=null)
            =>new OlympicResolution(current,source.SourceCards,source.GameId,source.ResolutionId,revision,source.SourceKind,source.Sponsor,source.RootPhasing,stage,branch,last,boycott,decision);
        static void RequireStage(OlympicResolution state,OlympicResolutionStage stage)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(state.Stage!=stage)throw new InvalidOperationException("Olympic stage does not allow this transition.");
        }
        static void RequireReply(OlympicResolution state,PlayerSide actor,long revision)
        {
            OlympicContestRules.RequireSide(actor);
            if(state.PendingOwner!=actor || state.Revision!=revision)throw new ArgumentException("Wrong Olympic owner or stale revision.");
        }
    }
}
