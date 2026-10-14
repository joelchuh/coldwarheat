using System;
namespace ColdWarStruggle.Core
{
    public enum CardActionKind { StartPractice, Influence, Space, FinishInfluence }
    public enum ActionPracticeStage { AwaitingStart, ChooseCard, PlacingInfluence, Complete }
    // Explicit single-turn, no-active-effects practice. It is not the full turn coordinator.
    public sealed class ActionPracticeDefinition
    {
        public CardCatalog Cards { get; }
        public OpsGeneralDefinition Ops { get; }
        public SpaceRaceDefinition Space { get; }
        public TurnCalendar Calendar { get; }
        public ActionPracticeDefinition(CardCatalog cards,OpsGeneralDefinition ops,SpaceRaceDefinition space,TurnCalendar calendar)
        { Cards=cards??throw new ArgumentNullException(nameof(cards));Ops=ops??throw new ArgumentNullException(nameof(ops));Space=space??throw new ArgumentNullException(nameof(space));Calendar=calendar??throw new ArgumentNullException(nameof(calendar)); }
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
        internal ActionPracticeState(ActionPracticeDefinition definition,ActionPracticeStage stage,int completed,CardId? card,SpaceAttemptState space,int vp)
        { Definition=definition;Stage=stage;CompletedActions=completed;ActiveCard=card;Space=space;VpForUS=vp; }
        public static ActionPracticeState Create(ActionPracticeDefinition definition)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            return new ActionPracticeState(definition,ActionPracticeStage.AwaitingStart,0,null,
                SpaceAttemptState.Create(new SpaceTrackState(definition.Space,definition.Space.InitialPosition,definition.Space.InitialPosition),definition.Calendar,1),0);
        }
    }
    public sealed class CardActionTransition
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public GameState NextState { get; }
        public SpaceAttemptReceipt SpaceReceipt { get; }
        internal CardActionTransition(RuleError error,GameState next=null,SpaceAttemptReceipt receipt=null)
        { Error=error;NextState=next;SpaceReceipt=receipt; }
    }
}
