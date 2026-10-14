using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum CardActionKind { StartPractice, Influence, Space, FinishInfluence, Coup, Scoring }
    public enum ActionPracticeStage { AwaitingStart, ChooseCard, PlacingInfluence, Complete }
    // Explicit single-turn, no-active-effects practice. It is not the full turn coordinator.
    public sealed class ActionPracticeDefinition
    {
        public CardCatalog Cards { get; }
        public OpsGeneralDefinition Ops { get; }
        public SpaceRaceDefinition Space { get; }
        public TurnCalendar Calendar { get; }
        public PracticeRiskDefinition Risk { get; }
        public RegionScoringDefinition Scoring { get; }
        public ActionPracticeDefinition(CardCatalog cards,OpsGeneralDefinition ops,SpaceRaceDefinition space,TurnCalendar calendar,PracticeRiskDefinition risk=null,RegionScoringDefinition scoring=null)
        {
            if(scoring!=null && (risk==null || !ReferenceEquals(scoring.Map,risk.Defcon.Map)))throw new ArgumentException("Scoring needs risk rules bound to the same map.");
            if(scoring!=null && cards!=null && cards.Cards.Where(c=>c.Kind==CardKind.Scoring).Any(c=>c.ScoringRegion!=scoring.WeightedRegionId && !scoring.Regions.Any(r=>r.Id==c.ScoringRegion)))throw new ArgumentException("Unknown printed scoring region.");
            Scoring=scoring;Cards=cards??throw new ArgumentNullException(nameof(cards));Ops=ops??throw new ArgumentNullException(nameof(ops));Space=space??throw new ArgumentNullException(nameof(space));Calendar=calendar??throw new ArgumentNullException(nameof(calendar));Risk=risk; }
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
        internal ActionPracticeState(ActionPracticeDefinition definition,ActionPracticeStage stage,int completed,CardId? card,SpaceAttemptState space,int vp,PracticeRiskState risk=null)
        { Definition=definition;Stage=stage;CompletedActions=completed;ActiveCard=card;Space=space;VpForUS=vp;Risk=risk; }
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
        internal CardActionTransition(RuleError error,GameState next=null,SpaceAttemptReceipt receipt=null,CoupAttemptReceipt coup=null,RegionScoringResult scoring=null)
        { ScoringResult=scoring;Error=error;NextState=next;SpaceReceipt=receipt;CoupReceipt=coup; }
    }
}
