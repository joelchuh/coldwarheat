using System;
namespace ColdWarStruggle.Core
{
    public enum OpponentEventOrder { EventFirst, OperationsFirst }
    public enum CardResolutionStep { Event, Operations, Complete }
    // A bounded two-step root, not a general nested-card stack or serialized continuation.
    public sealed class OpponentOpsSequence
    {
        public CardId Card { get; }
        public PlayerSide Actor { get; }
        public PlayerSide EventSide=>Actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
        public OpponentEventOrder Order { get; }
        public int CompletedSteps { get; }
        public OperationsMode? Mode { get; }
        public OpponentOpsSequence SelectMode(OperationsMode mode)
        {
            if(!Enum.IsDefined(typeof(OperationsMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            if(CurrentStep!=CardResolutionStep.Operations || Mode.HasValue)throw new InvalidOperationException("OPS mode can only be chosen once at its current step.");
            return new OpponentOpsSequence(Card,Actor,Order,CompletedSteps,mode);
        }
        public CardResolutionStep CurrentStep=>CompletedSteps==2?CardResolutionStep.Complete
            :(Order==OpponentEventOrder.EventFirst)==(CompletedSteps==0)?CardResolutionStep.Event:CardResolutionStep.Operations;
        public bool EventOccurred=>CompletedSteps==2 || CompletedSteps==1 && Order==OpponentEventOrder.EventFirst;
        OpponentOpsSequence(CardId card,PlayerSide actor,OpponentEventOrder order,int completed,OperationsMode? mode)
        { Card=card;Actor=actor;Order=order;CompletedSteps=completed;Mode=mode; }
        public static OpponentOpsSequence Create(CardId card,PlayerSide actor,OpponentEventOrder order,OperationsMode? mode=OperationsMode.Influence)
        {
            if(card.Value==null)throw new ArgumentException("A physical card ID is required.");
            if(actor!=PlayerSide.US && actor!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(actor));
            if(order!=OpponentEventOrder.EventFirst && order!=OpponentEventOrder.OperationsFirst)throw new ArgumentOutOfRangeException(nameof(order));
            if(mode.HasValue && !Enum.IsDefined(typeof(OperationsMode),mode.Value))throw new ArgumentOutOfRangeException(nameof(mode));
            return new OpponentOpsSequence(card,actor,order,0,mode);
        }
        public OpponentOpsSequence Advance(CardResolutionStep step)
        {
            if(CurrentStep==CardResolutionStep.Complete || step!=CurrentStep)throw new InvalidOperationException("Only the current incomplete card step can advance.");
            if(step==CardResolutionStep.Operations && !Mode.HasValue)throw new InvalidOperationException("Choose and resolve an OPS mode first.");
            return new OpponentOpsSequence(Card,Actor,Order,CompletedSteps+1,Mode);
        }
    }
}
