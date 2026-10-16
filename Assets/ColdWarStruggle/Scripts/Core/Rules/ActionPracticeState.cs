using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum CardActionKind { StartPractice, Influence, Space, FinishInfluence, Coup, Scoring, Event, Realignment, RollRealignment, FinishRealignment, EventThenInfluence, InfluenceThenEvent, OpponentEventFirst, OpponentOpsFirst, UseOpponentInfluence, UseOpponentRealignment, UseOpponentCoup, AssessTurnEnd, BeginNextTurn, FinalScoring, DiscardHeldCard, KeepHeldCards, DeclineExtraActions, SkipEmptyHand, DiscardForBlockade, DeclineBlockade, ResolveWar }
    public enum ActionPracticeStage { AwaitingStart, ChooseCard, PlacingInfluence, Complete, ResolvingEvent, Realigning, ChoosingOperations, HeadlineReady, CollectingHeadline }
    // Explicit single-turn practice with a bounded, typed set of supported events. Not the full turn coordinator.
    public sealed class ActionPracticeDefinition
    {
        public WarEventCatalog Wars { get; }
        public FiveYearPlanDefinition FiveYearPlan { get; }
        public BlockadeDefinition Blockade { get; }
        public CardCatalog Cards { get; }
        public OpsGeneralDefinition Ops { get; }
        public SpaceRaceDefinition Space { get; }
        public TurnCalendar Calendar { get; }
        public PracticeRiskDefinition Risk { get; }
        public RegionScoringDefinition Scoring { get; }
        public PracticeInfluenceCatalog InfluenceEvents { get; }
        public PracticeDefconVpCatalog DefconVpEvents { get; }
        public PracticeTurnOpsCatalog TurnOpsEvents { get; }
        public RealignmentDefinition Realignment { get; }
        public ChinaCardDefinition China { get; }
        public bool TurnEndEnabled { get; }
        public bool NextTurnEnabled { get; }
        public HeadlineOrderParameters HeadlineParameters { get; }
        public DefectorsHeadlineDefinition HeadlineDefectors { get; }
        public FinalScoringDefinition FinalScoring { get; }
        public ActionPracticeDefinition(CardCatalog cards,OpsGeneralDefinition ops,SpaceRaceDefinition space,TurnCalendar calendar,PracticeRiskDefinition risk=null,RegionScoringDefinition scoring=null,PracticeInfluenceCatalog influenceEvents=null,PracticeDefconVpCatalog defconVpEvents=null,PracticeTurnOpsCatalog turnOpsEvents=null,RealignmentDefinition realignment=null,ChinaCardDefinition china=null,bool turnEndEnabled=false,bool nextTurnEnabled=false,HeadlineOrderParameters headlineParameters=null,DefectorsHeadlineDefinition headlineDefectors=null,FinalScoringDefinition finalScoring=null,BlockadeDefinition blockade=null,FiveYearPlanDefinition fiveYearPlan=null,WarEventCatalog wars=null)
        {
            if(wars!=null && (risk==null || !ReferenceEquals(wars.Cards,cards) || !ReferenceEquals(wars.Map,risk.Defcon.Map)
                || wars.Effects.Keys.Any(id=>influenceEvents?.Effects.ContainsKey(id)==true || defconVpEvents?.Effects.ContainsKey(id)==true
                    || turnOpsEvents?.Effects.ContainsKey(id)==true || headlineDefectors?.Card.Equals(id)==true || blockade?.Card.Id.Equals(id)==true || fiveYearPlan?.Card.Id.Equals(id)==true)))throw new ArgumentException("Wars require shared risk, map, cards and unique handlers.");
            if(fiveYearPlan!=null && (!ReferenceEquals(fiveYearPlan.Cards,cards) || influenceEvents?.Effects.ContainsKey(fiveYearPlan.Card.Id)==true
                || defconVpEvents?.Effects.ContainsKey(fiveYearPlan.Card.Id)==true || turnOpsEvents?.Effects.ContainsKey(fiveYearPlan.Card.Id)==true
                || headlineDefectors?.Card.Equals(fiveYearPlan.Card.Id)==true || blockade?.Card.Id.Equals(fiveYearPlan.Card.Id)==true))throw new ArgumentException("Five Year Plan must share the catalog and a unique handler.");
            if(blockade!=null && (risk==null || !ReferenceEquals(blockade.Cards,cards) || !ReferenceEquals(blockade.Ops,ops) || !ReferenceEquals(blockade.Map,risk.Defcon.Map)
                || influenceEvents?.Effects.ContainsKey(blockade.Card.Id)==true || defconVpEvents?.Effects.ContainsKey(blockade.Card.Id)==true
                || turnOpsEvents?.Effects.ContainsKey(blockade.Card.Id)==true || headlineDefectors?.Card.Equals(blockade.Card.Id)==true))throw new ArgumentException("Blockade must share exact definitions and a unique handler.");
            if(influenceEvents!=null && (!ReferenceEquals(influenceEvents.Cards,cards) || (risk!=null && !ReferenceEquals(influenceEvents.Map,risk.Defcon.Map)) || (scoring!=null && !ReferenceEquals(influenceEvents.Map,scoring.Map))))throw new ArgumentException("Influence events must share the same cards and map.");
            if(scoring!=null && (risk==null || !ReferenceEquals(scoring.Map,risk.Defcon.Map)))throw new ArgumentException("Scoring needs risk rules bound to the same map.");
            if(scoring!=null && cards!=null && cards.Cards.Where(c=>c.Kind==CardKind.Scoring).Any(c=>c.ScoringRegion!=scoring.WeightedRegionId && !scoring.Regions.Any(r=>r.Id==c.ScoringRegion)))throw new ArgumentException("Unknown printed scoring region.");
            if(defconVpEvents!=null && (risk==null || !ReferenceEquals(defconVpEvents.Cards,cards) || influenceEvents!=null && defconVpEvents.Effects.Keys.Any(influenceEvents.Effects.ContainsKey)))throw new ArgumentException("DEFCON events need same cards, risk and disjoint effect IDs.");
            if(turnOpsEvents!=null && (!ReferenceEquals(turnOpsEvents.Cards,cards) || turnOpsEvents.Effects.Keys.Any(id=>influenceEvents?.Effects.ContainsKey(id)==true || defconVpEvents?.Effects.ContainsKey(id)==true)))throw new ArgumentException("Flat OPS events require same cards and disjoint IDs.");
            if(realignment!=null && risk==null)throw new ArgumentException("Realignment requires DEFCON risk rules.");
            if(china!=null && (!ReferenceEquals(china.Cards,cards) || risk!=null && !ReferenceEquals(china.Map,risk.Defcon.Map)))throw new ArgumentException("China rules must share the same cards and map.");
            if(finalScoring!=null && (!turnEndEnabled || !ReferenceEquals(finalScoring.RegionDefinition,scoring) || calendar?.LastTurn!=10))throw new ArgumentException("Final scoring requires shared region rules and an assessed ten-turn calendar.");
            if(nextTurnEnabled && !turnEndEnabled)throw new ArgumentException("Next turn requires turn-end assessment.");
            if(turnEndEnabled && (risk==null || china==null))throw new ArgumentException("Turn-end practice requires risk and China rules.");
            if(headlineDefectors!=null && (!ReferenceEquals(headlineDefectors.Catalog,cards) || influenceEvents?.Effects.ContainsKey(headlineDefectors.Card)==true || defconVpEvents?.Effects.ContainsKey(headlineDefectors.Card)==true || turnOpsEvents?.Effects.ContainsKey(headlineDefectors.Card)==true))throw new ArgumentException("Headline Defectors requires same cards and disjoint handlers.");
            Wars=wars;FiveYearPlan=fiveYearPlan;Blockade=blockade;FinalScoring=finalScoring;HeadlineDefectors=headlineDefectors;HeadlineParameters=headlineParameters;NextTurnEnabled=nextTurnEnabled;TurnEndEnabled=turnEndEnabled;China=china;Realignment=realignment;TurnOpsEvents=turnOpsEvents;DefconVpEvents=defconVpEvents;InfluenceEvents=influenceEvents;Scoring=scoring;Cards=cards??throw new ArgumentNullException(nameof(cards));Ops=ops??throw new ArgumentNullException(nameof(ops));Space=space??throw new ArgumentNullException(nameof(space));Calendar=calendar??throw new ArgumentNullException(nameof(calendar));Risk=risk; }
    }
    public sealed class ActionPracticeState
    {
        public WarDecision War { get; }
        public FiveYearPlanExecution FiveYearPlan { get; }
        public BlockadeDecision Blockade { get; }
        public ActionPracticeDefinition Definition { get; }
        public ActionPracticeStage Stage { get; }
        public int CompletedActions => Rounds.TotalCompleted;
        public ActionRoundProgress Rounds { get; }
        public HeadlineResolutionState Headline { get; }
        public HeadlineSelectionState HeadlineSelection { get; }
        public bool HasPendingHeadline=>Headline!=null && !Headline.IsComplete;
        public PlayerSide Actor=>HasPendingHeadline?Headline.Current.PlayedBy:Rounds.CurrentActor??PlayerSide.USSR;
        public int ActionRound=>Rounds.Round;
        public CardId? ActiveCard { get; }
        public SpaceAttemptState Space { get; }
        public int VpForUS { get; }
        public PracticeRiskState Risk { get; }
        public TurnOpsEffectState OpsEffects { get; }
        public OpponentOpsSequence OpponentOps { get; }
        public PracticeTurnEndReceipt TurnEnd { get; }
        public PracticeFinalScoringReceipt FinalScoring { get; }
        public SpaceEndTurnDiscardState SpaceDiscard { get; }
        internal ActionPracticeState(ActionPracticeDefinition definition,ActionPracticeStage stage,int completed,CardId? card,SpaceAttemptState space,int vp,PracticeRiskState risk=null,TurnOpsEffectState opsEffects=null,OpponentOpsSequence opponentOps=null,PracticeTurnEndReceipt turnEnd=null,HeadlineResolutionState headline=null,HeadlineSelectionState headlineSelection=null,PracticeFinalScoringReceipt finalScoring=null,SpaceEndTurnDiscardState spaceDiscard=null,ActionRoundProgress rounds=null,BlockadeDecision blockade=null,FiveYearPlanExecution fiveYearPlan=null,WarDecision war=null)
        {
            if(rounds==null && completed!=0)throw new ArgumentException("Existing root progress must be carried explicitly.");
            Rounds=rounds??ActionRoundProgress.Create(space.Turn,definition.Space);
            if(Rounds.TotalCompleted!=completed || Rounds.Turn.Number!=space.Turn.Number || !ReferenceEquals(Rounds.Turn.Parameters,space.Turn.Parameters)
                || !ReferenceEquals(Rounds.SpaceDefinition,space.Track.Definition) || !ReferenceEquals(Rounds.SpaceDefinition,definition.Space))throw new ArgumentException("Action progress binding mismatch.");
            if(headline!=null && (headline.Turn.Number!=space.Turn.Number || headline.Turn.Era!=space.Turn.Era))throw new ArgumentException("Headline turn mismatch.");
            if(headline!=null && !headline.IsComplete && (completed!=0 || opponentOps!=null || turnEnd!=null
                || !(stage==ActionPracticeStage.HeadlineReady && !card.HasValue
                    || stage==ActionPracticeStage.ResolvingEvent && card.HasValue && card.Value.Equals(headline.Current.Card)
                    || stage==ActionPracticeStage.Complete && risk?.Result?.Outcome==VictoryOutcome.Finished && card.HasValue && card.Value.Equals(headline.Current.Card))))
                throw new ArgumentException("Invalid pending headline root.");
            if(stage==ActionPracticeStage.HeadlineReady && (headline==null || headline.IsComplete))throw new ArgumentException("Headline boundary requires a pending root.");
            if(headlineSelection!=null && (definition.HeadlineParameters==null || !ReferenceEquals(headlineSelection.Catalog,definition.Cards)
                || headlineSelection.Ready || headline!=null || stage!=ActionPracticeStage.CollectingHeadline || completed!=0
                || card.HasValue || opponentOps!=null || turnEnd!=null))throw new ArgumentException("Invalid headline collection.");
            if(stage==ActionPracticeStage.CollectingHeadline && headlineSelection==null)throw new ArgumentException("Missing headline selection.");
            HeadlineSelection=headlineSelection;Headline=headline;
            if(turnEnd!=null && (!definition.TurnEndEnabled || stage!=ActionPracticeStage.Complete || !Rounds.IsComplete || card.HasValue || opponentOps!=null
                || !ReferenceEquals(risk?.Military.Assessment,turnEnd.Assessment)
                || (finalScoring==null && (turnEnd.Decision.SignedVp!=vp || (turnEnd.Decision.Outcome==VictoryOutcome.Finished?!ReferenceEquals(risk.Result,turnEnd.Decision):risk.Result!=null)))))throw new ArgumentException("Turn-end receipt does not match the completed state.");
            if(finalScoring!=null && (definition.FinalScoring==null || turnEnd==null || turnEnd.Decision.Outcome!=VictoryOutcome.Continue
                || space.Turn.Number!=10 || finalScoring.BeforeVp!=turnEnd.Decision.SignedVp || finalScoring.Decision.SignedVp!=vp
                || finalScoring.Decision.Checkpoint!=VictoryCheckpoint.FinalScoringCompleted || finalScoring.Decision.Outcome!=VictoryOutcome.Finished
                || !ReferenceEquals(finalScoring.Result.Definition,definition.FinalScoring) || !ReferenceEquals(risk?.Result,finalScoring.Decision)))
                throw new ArgumentException("Final receipt must extend the continuing turn-end audit with one terminal decision.");
            if(spaceDiscard!=null && (turnEnd?.Decision.Outcome!=VictoryOutcome.Continue || !ReferenceEquals(spaceDiscard.Track,space.Track)
                || !ReferenceEquals(spaceDiscard.Audit,turnEnd) || finalScoring!=null && spaceDiscard.Status==SpaceEndTurnDiscardStatus.Pending))throw new ArgumentException("Discard decision must extend this audit and track.");
            if(turnEnd?.Decision.Outcome==VictoryOutcome.Continue)
            {
                if(SpaceEndTurnDiscardState.QueryOwner(space.Track,out var owner)!=RuleError.None || owner!=spaceDiscard?.Owner)throw new ArgumentException("Missing or foreign space discard decision.");
            }
            if(war!=null && (blockade!=null || fiveYearPlan!=null || turnEnd!=null || headlineSelection!=null || stage!=ActionPracticeStage.ResolvingEvent
                || !ReferenceEquals(war.Catalog,definition.Wars) || !card.HasValue || !card.Value.Equals(war.Effect.Card)))throw new ArgumentException("Invalid pending war root.");
            if(blockade!=null && (!ReferenceEquals(blockade.Definition,definition.Blockade) || stage!=ActionPracticeStage.ResolvingEvent
                || !card.HasValue || !card.Value.Equals(blockade.Definition.Card.Id) || turnEnd!=null || headlineSelection!=null))throw new ArgumentException("Invalid blockade root.");
            if(fiveYearPlan!=null && (blockade!=null || turnEnd!=null || headlineSelection!=null || !ReferenceEquals(fiveYearPlan.Definition,definition.FiveYearPlan)))throw new ArgumentException("Invalid Five Year Plan parent.");
            War=war;FiveYearPlan=fiveYearPlan;Blockade=blockade;SpaceDiscard=spaceDiscard;FinalScoring=finalScoring;TurnEnd=turnEnd;OpponentOps=opponentOps;Definition=definition;Stage=stage;ActiveCard=card;Space=space;VpForUS=vp;Risk=risk;OpsEffects=opsEffects??TurnOpsEffectState.Create(space.Turn);
            if(fiveYearPlan!=null && !fiveYearPlan.MatchesRoot(this))throw new ArgumentException("Five Year Plan continuation changed its parent root."); }
        // Trusted scenario initialization only: no completed actions or fabricated audit history.
        public static ActionPracticeState CreateAtTurn(ActionPracticeDefinition definition,int turnNumber,int initialVp,SpaceTrackState initialTrack=null)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            var turn=definition.Calendar.ForTurn(turnNumber);
            if(definition.Risk==null?initialVp!=0:initialVp<=-(long)definition.Risk.Victory.AutomaticVpThreshold || initialVp>=definition.Risk.Victory.AutomaticVpThreshold)
                throw new ArgumentOutOfRangeException(nameof(initialVp));
            if(initialTrack!=null && !ReferenceEquals(initialTrack.Definition,definition.Space))throw new ArgumentException("Initial space track must use the shared definition.");
            var space=SpaceAttemptState.Create(initialTrack??new SpaceTrackState(definition.Space,definition.Space.InitialPosition,definition.Space.InitialPosition),definition.Calendar,turnNumber);
            return new ActionPracticeState(definition,ActionPracticeStage.AwaitingStart,0,null,space,initialVp,
                definition.Risk==null?null:PracticeRiskState.Create(definition.Risk,space.Turn));
        }
        public static ActionPracticeState Create(ActionPracticeDefinition definition)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            return CreateAtTurn(definition,1,definition.Risk?.Victory.InitialSignedVp??0);
        }
    }
    public sealed class CardActionTransition
    {
        public WarEventPlan WarResult { get; }
        public FiveYearPlanResult FiveYearPlanResult { get; }
        public BlockadeResolutionResult BlockadeResult { get; }
        public System.Collections.Generic.IReadOnlyList<HeadlineResolutionStep> HeadlineSteps { get; }
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public GameState NextState { get; }
        public SpaceAttemptReceipt SpaceReceipt { get; }
        public CoupAttemptReceipt CoupReceipt { get; }
        public RegionScoringResult ScoringResult { get; }
        public InfluenceEventTransition EventTransition { get; }
        public DefconVpEventPlan DefconVpEvent { get; }
        public TurnOpsEffectInstance ActivatedOpsEffect { get; }
        public RealignmentCalculation Realignment { get; }
        internal CardActionTransition(RuleError error,GameState next=null,SpaceAttemptReceipt receipt=null,CoupAttemptReceipt coup=null,RegionScoringResult scoring=null,InfluenceEventTransition eventTransition=null,DefconVpEventPlan defconVpEvent=null,TurnOpsEffectInstance activatedOpsEffect=null,RealignmentCalculation realignment=null,BlockadeResolutionResult blockadeResult=null,System.Collections.Generic.IReadOnlyList<HeadlineResolutionStep> headlineSteps=null,FiveYearPlanResult fiveYearPlanResult=null,WarEventPlan warResult=null)
        { WarResult=warResult;FiveYearPlanResult=fiveYearPlanResult;BlockadeResult=blockadeResult;HeadlineSteps=Array.AsReadOnly((headlineSteps??Array.Empty<HeadlineResolutionStep>()).ToArray());Realignment=realignment;ActivatedOpsEffect=activatedOpsEffect;DefconVpEvent=defconVpEvent;EventTransition=eventTransition;ScoringResult=scoring;Error=error;NextState=next;SpaceReceipt=receipt;CoupReceipt=coup; }
    }
}
