using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
namespace ColdWarStruggle.Core
{
    public sealed class InfluenceEventFactDefinition
    {
        public MapDefinition Map { get; }
        public CardCatalog Cards { get; }
        public IReadOnlyDictionary<CardId,InfluenceEffectDefinition> Effects { get; }
        public string RulesetId { get; }
        public string ContentVersion { get; }
        public string DataDigest { get; }
        public InfluenceEventFactDefinition(MapDefinition map,CardCatalog cards,IEnumerable<InfluenceEffectDefinition> effects,string ruleset,string contentVersion,string digest)
        {
            if(map==null || cards==null || effects==null)throw new ArgumentNullException();
            if(ruleset!="ruleset.deluxe2015" || contentVersion!="0.1.0" || digest==null
                || !Regex.IsMatch(digest,@"\A[0-9a-f]{64}\z",RegexOptions.CultureInvariant))throw new ArgumentException("Unsupported event fact profile.");
            var copy=effects.Take(cards.Cards.Count+1).ToArray();
            if(copy.Length==0 || copy.Length>cards.Cards.Count || copy.Any(e=>e==null)
                || copy.Select(e=>e.CardId).Distinct(StringComparer.Ordinal).Count()!=copy.Length)throw new ArgumentException("Invalid fact coverage.");
            var bindings=new Dictionary<CardId,InfluenceEffectDefinition>();
            foreach(var effect in copy.OrderBy(e=>e.CardId,StringComparer.Ordinal))
            {
                var id=new CardId(effect.CardId);
                if(!cards.Contains(id))throw new ArgumentException("Unknown fact card.");
                var printed=cards.GetCard(id);
                var affiliation=effect.Beneficiary==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR;
                if(printed.Kind!=CardKind.Event || printed.Effect.EffectId!=effect.EffectId || printed.Affiliation!=affiliation)
                    throw new ArgumentException("Fact coverage does not match the printed card.");
                bindings.Add(id,effect);
            }
            Map=map;Cards=cards;Effects=new ReadOnlyDictionary<CardId,InfluenceEffectDefinition>(bindings);
            RulesetId=ruleset;ContentVersion=contentVersion;DataDigest=digest;
        }
    }
}
