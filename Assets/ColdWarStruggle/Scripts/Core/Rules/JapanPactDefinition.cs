using System;
using System.Text.RegularExpressions;
namespace ColdWarStruggle.Core
{
    public sealed class JapanPactDefinition
    {
        public MapDefinition Map { get; }
        public CardCatalog Cards { get; }
        public CardId Card { get; }
        public string EffectId { get; }
        public string RulesetId { get; }
        public string ContentVersion { get; }
        public string DataDigest { get; }
        public AddToControlInfluenceEffect ImmediateInfluence { get; }
        public string Duration => "UntilCancelledOrGameEnd";
        public JapanPactDefinition(MapDefinition map,CardCatalog cards,AddToControlInfluenceEffect immediate,string ruleset,string version,string digest)
        {
            if(map==null || cards==null || immediate==null)throw new ArgumentNullException();
            if(immediate.CardId!="card.us_japan_mutual_defense_pact" || immediate.EffectId!="effect.us_japan_mutual_defense_pact"
                || immediate.Beneficiary!=PlayerSide.US || immediate.Country.Value!="country.japan" || !map.Contains(immediate.Country)
                || ruleset!="ruleset.deluxe2015" || version!="0.1.0" || digest==null || !Regex.IsMatch(digest,@"\A[0-9a-f]{64}\z",RegexOptions.CultureInvariant))
                throw new ArgumentException("Unsupported Japan pact definition.");
            var card=new CardId(immediate.CardId);var printed=cards.GetCard(card);var binding=printed.Effect;
            if(printed.Kind!=CardKind.Event || printed.Affiliation!=CardAffiliation.US || !printed.RemoveAfterResolvedEvent || !printed.DisplayPersistentReminder
                || binding==null || binding.EffectId!=immediate.EffectId || binding.ContractVersion!=version || binding.Kind!=CardParameterBindingKind.Bound
                || binding.Parameters.Count!=1 || binding.Parameters[0].DatasetId!="effects.deluxe2015.treaty_protection"
                || binding.Parameters[0].JsonPointer!="/parameters/us_japan_mutual_defense_pact")throw new ArgumentException("Invalid Japan pact printed binding.");
            Map=map;Cards=cards;Card=card;EffectId=immediate.EffectId;ImmediateInfluence=immediate;
            RulesetId=ruleset;ContentVersion=version;DataDigest=digest;
        }
    }
}
