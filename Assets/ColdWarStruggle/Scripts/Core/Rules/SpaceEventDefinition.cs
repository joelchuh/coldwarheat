using System;
namespace ColdWarStruggle.Core
{
    public sealed class SpaceEventDefinition
    {
        public CardCatalog Cards { get; }
        public CardDefinition Card { get; }
        public SpaceRaceDefinition Track { get; }
        public string EffectId { get; }
        public int AdvanceSteps { get; }
        public SpaceEventDefinition(CardCatalog cards,SpaceRaceDefinition track,CardId card,string effectId,int advanceSteps)
        {
            if(cards==null || track==null)throw new ArgumentNullException();
            if(card.Value!="card.captured_nazi_scientist" || effectId!="effect.captured_nazi_scientist" || advanceSteps!=1 || !cards.Contains(card))
                throw new ArgumentException("Unsupported single-step space event.");
            var printed=cards.GetCard(card);
            if(printed.Kind!=CardKind.Event || printed.PrintedNumber!=18 || printed.PrintedOps!=1 || printed.Affiliation!=CardAffiliation.Neutral
                || printed.Era!=GameEra.Early || !printed.RemoveAfterResolvedEvent || printed.DisplayPersistentReminder
                || printed.Effect.EffectId!=effectId || printed.Effect.ContractVersion!="0.1.0" || printed.Effect.Kind!=CardParameterBindingKind.Bound
                || printed.Effect.Parameters.Count!=1 || printed.Effect.Parameters[0].DatasetId!="effects.deluxe2015.space_events_batch1"
                || printed.Effect.Parameters[0].JsonPointer!="/parameters/captured_nazi_scientist")throw new ArgumentException("Printed card does not bind this space event.");
            Cards=cards;Track=track;Card=printed;EffectId=effectId;AdvanceSteps=advanceSteps;
        }
    }
}
