using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class BlockadeDefinition
    {
        public MapDefinition Map { get; }
        public CardCatalog Cards { get; }
        public OpsGeneralDefinition Ops { get; }
        public CardDefinition Card { get; }
        public string EffectId { get; }
        public PlayerSide Owner { get; }
        public int MinimumEffectiveOps { get; }
        public CountryId FallbackCountry { get; }
        public BlockadeDefinition(MapDefinition map,CardCatalog cards,OpsGeneralDefinition ops,CardId cardId,string effectId,PlayerSide owner,int minimumEffectiveOps,CountryId fallbackCountry)
        {
            if(map==null || cards==null || ops==null)throw new ArgumentNullException();
            if(owner!=PlayerSide.US || !map.Contains(fallbackCountry) || minimumEffectiveOps<ops.Minimum || minimumEffectiveOps>ops.OrdinaryMaximum
                || !ops.FlatPurposes.Contains(OpsValuePurpose.OpsThreshold))throw new ArgumentException("Invalid discard-or-remove definition context.");
            var card=cards.GetCard(cardId);
            if(card.Kind!=CardKind.Event || card.Affiliation!=CardAffiliation.USSR || !card.RemoveAfterResolvedEvent
                || string.IsNullOrWhiteSpace(effectId) || card.Effect.EffectId!=effectId || card.Effect.ContractVersion!="0.1.0"
                || card.Effect.Kind!=CardParameterBindingKind.Bound || card.Effect.Parameters.Count!=1
                || card.Effect.Parameters[0].DatasetId!="effects.deluxe2015.hand_events_batch1"
                || card.Effect.Parameters[0].JsonPointer!="/parameters/blockade")throw new ArgumentException("Printed card does not bind this discard-or-remove effect.");
            Map=map;Cards=cards;Ops=ops;Card=card;EffectId=effectId;Owner=owner;MinimumEffectiveOps=minimumEffectiveOps;FallbackCountry=fallbackCountry;
        }
    }
}
