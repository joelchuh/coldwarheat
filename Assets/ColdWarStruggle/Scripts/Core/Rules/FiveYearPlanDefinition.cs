using System;
namespace ColdWarStruggle.Core
{
    public sealed class FiveYearPlanDefinition
    {
        public CardCatalog Cards { get; }
        public CardDefinition Card { get; }
        public string EffectId { get; }
        public PlayerSide HandOwner { get; }
        public CardAffiliation TriggeredAffiliation { get; }
        public int DiscardCount { get; }
        public FiveYearPlanDefinition(CardCatalog cards,CardId card,string effectId,PlayerSide handOwner,CardAffiliation triggeredAffiliation,int discardCount)
        {
            if(cards==null)throw new ArgumentNullException(nameof(cards));
            if(handOwner!=PlayerSide.USSR || triggeredAffiliation!=CardAffiliation.US || discardCount!=1 || !cards.Contains(card))throw new ArgumentException("Unsupported random hand event semantics.");
            var printed=cards.GetCard(card);
            if(printed.Kind!=CardKind.Event || printed.Affiliation!=CardAffiliation.US || printed.RemoveAfterResolvedEvent
                || string.IsNullOrWhiteSpace(effectId) || printed.Effect.EffectId!=effectId || printed.Effect.ContractVersion!="0.1.0"
                || printed.Effect.Kind!=CardParameterBindingKind.Bound || printed.Effect.Parameters.Count!=1
                || printed.Effect.Parameters[0].DatasetId!="effects.deluxe2015.hand_events_batch1"
                || printed.Effect.Parameters[0].JsonPointer!="/parameters/five_year_plan")throw new ArgumentException("Printed card does not bind this random hand event.");
            Cards=cards;Card=printed;EffectId=effectId;HandOwner=handOwner;TriggeredAffiliation=triggeredAffiliation;DiscardCount=discardCount;
        }
    }
}
