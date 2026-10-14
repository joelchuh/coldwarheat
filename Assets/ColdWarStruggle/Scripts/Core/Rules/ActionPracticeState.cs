using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum CardActionKind { StartPractice, Influence, Space, FinishInfluence, Coup, Scoring, Event, Realignment, RollRealignment, FinishRealignment, EventThenInfluence, InfluenceThenEvent, OpponentEventFirst, OpponentOpsFirst, UseOpponentInfluence, UseOpponentRealignment, UseOpponentCoup, AssessTurnEnd }
    public enum ActionPracticeStage { AwaitingStart, ChooseCard, PlacingInfluence, Complete, ResolvingEvent, Realigning, ChoosingOperations }
    // Explicit single-turn practice with a bounded, typed set of supported events. Not the full turn coordinator.
    public sealed class ActionPracticeDefinition
    {
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
        public ActionPracticeDefinition(CardCatalog cards,OpsGeneralDefinition ops,SpaceRaceDefinition space,TurnCalendar calendar,PracticeRiskDefinition risk=null,RegionScoringDefinition scoring=null,PracticeInfluenceCatalog influenceEvents=null,PracticeDefconVpCatalog defconVpEvents=null,PracticeTurnOpsCatalog turnOpsEvents=null,RealignmentDefinition realignment=null,ChinaCardDefinition china=null,bool turnEndEnabled=false)
        {
            if(influenceEvents!=null && (!ReferenceEquals(influenceEvents.Cards,cards) || (risk!=null && !ReferenceEquals(influenceEvents.Map,risk.Defcon.Map)) || (scoring!=null && !ReferenceEquals(influenceEvents.Map,scoring.Map))))throw new ArgumentException("Influence events must share the same cards and map.");
            if(scoring!=null && (risk==null || !ReferenceEquals(scoring.Map,risk.Defcon.Map)))throw new ArgumentException("Scoring needs risk rules bound to the same map.");
            if(scoring!=null && cards!=null && cards.Cards.Where(c=>c.Kind==CardKind.Scoring).Any(c=>c.ScoringRegion!=scoring.WeightedRegionId && !scoring.Regions.Any(r=>r.Id==c.ScoringRegion)))throw new ArgumentException("Unknown printed scoring region.");
            if(defconVpEvents!=null && (risk==null || !ReferenceEquals(defconVpEvents.Cards,cards) || influenceEvents!=null && defconVpEvents.Effects.Keys.Any(influenceEvents.Effects.ContainsKey)))throw new ArgumentException("DEFCON events need same cards, risk and disjoint effect IDs.");
            if(turnOpsEvents!=null && (!ReferenceEquals(turnOpsEvents.Cards,cards) || turnOpsEvents.Effects.Keys.Any(id=>influenceEvents?.Effects.ContainsKey(id)==true || defconVpEvents?.Effects.ContainsKey(id)==true)))throw new ArgumentException("Flat OPS events require same cards and disjoint IDs.");
            if(realignment!=null && risk==null)throw new ArgumentException("Realignment requires DEFCON risk rules.");
            if(china!=null && (!ReferenceEquals(china.Cards,cards) || risk!=null && !ReferenceEquals(china.Map,risk.Defcon.Map)))throw new ArgumentException("China rules must share the same cards and map.");
            if(turnEndEnabled && (risk==null || china==null))throw new ArgumentException("Turn-end practice requires risk and China rules.");
            TurnEndEnabled=turnEndEnabled;China=china;Realignment=realignment;TurnOpsEvents=turnOpsEvents;DefconVpEvents=defconVpEvents;InfluenceEvents=influenceEvents;Scoring=scoring;Cards=cards??throw new ArgumentNullException(nameof(cards));Ops=ops??throw new ArgumentNullException(nameof(ops));Space=space??throw new ArgumentNullException(nameof(space));Calendar=calendar??throw new ArgumentNullException(nameof(calendar));Risk=risk; }
    }
    public sealed class ActionPracticeState
    {
        public ActionPracticeDefinition Definition { get; }
        public ActionPracticeStage Stage { get; }
        public int CompletedActions { get; }
        public PlayerSide Actor=>CompletedActions%2==0?PlayerSide.USSR:PlayerSide.US;
        public int ActionRound=>Math.Min(CompletedActions/2+1,Space.Turn.Parameters.NormalActionsPerSide);
        public CardId? ActiveCard { get; }
        public SpaceAttemptState Space { get; }
        public int VpForUS { get; }
        public PracticeRiskState Risk { get; }
        public TurnOpsEffectState OpsEffects { get; }
        public OpponentOpsSequence OpponentOps { get; }
        public PracticeTurnEndReceipt TurnEnd { get; }
        internal ActionPracticeState(ActionPracticeDefinition definition,ActionPracticeStage stage,int completed,CardId? card,SpaceAttemptState space,int vp,PracticeRiskState risk=null,TurnOpsEffectState opsEffects=null,OpponentOpsSequence opponentOps=null,PracticeTurnEndReceipt turnEnd=null)
        {
            if(turnEnd!=null && (!definition.TurnEndEnabled || stage!=ActionPracticeStage.Complete || completed!=space.Turn.Parameters.NormalActionsPerSide*2 || card.HasValue || opponentOps!=null
                || !ReferenceEquals(risk?.Military.Assessment,turnEnd.Assessment) || turnEnd.Decision.SignedVp!=vp
                || (turnEnd.Decision.Outcome==VictoryOutcome.Finished?!ReferenceEquals(risk.Result,turnEnd.Decision):risk.Result!=null)))throw new ArgumentException("Turn-end receipt does not match the completed state.");
            TurnEnd=turnEnd;OpponentOps=opponentOps;Definition=definition;Stage=stage;CompletedActions=completed;ActiveCard=card;Space=space;VpForUS=vp;Risk=risk;OpsEffects=opsEffects??TurnOpsEffectState.Create(space.Turn); }
        public static ActionPracticeState Create(ActionPracticeDefinition definition)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            return new ActionPracticeState(definition,ActionPracticeStage.AwaitingStart,0,null,
                SpaceAttemptState.Create(new SpaceTrackState(definition.Space,definition.Space.InitialPosition,definition.Space.InitialPosition),definition.Calendar,1),definition.Risk?.Victory.InitialSignedVp??0,definition.Risk==null?null:PracticeRiskState.Create(definition.Risk,definition.Calendar.ForTurn(1)));
        }
    }
    public sealed class CardActionTransition
    {
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
        internal CardActionTransition(RuleError error,GameState next=null,SpaceAttemptReceipt receipt=null,CoupAttemptReceipt coup=null,RegionScoringResult scoring=null,InfluenceEventTransition eventTransition=null,DefconVpEventPlan defconVpEvent=null,TurnOpsEffectInstance activatedOpsEffect=null,RealignmentCalculation realignment=null)
        { Realignment=realignment;ActivatedOpsEffect=activatedOpsEffect;DefconVpEvent=defconVpEvent;EventTransition=eventTransition;ScoringResult=scoring;Error=error;NextState=next;SpaceReceipt=receipt;CoupReceipt=coup; }
    }
}
