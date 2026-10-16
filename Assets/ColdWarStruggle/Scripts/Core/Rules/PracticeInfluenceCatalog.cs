using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Only implemented pure influence effects; this is not the complete game's event registry.
    public sealed class PracticeInfluenceCatalog
    {
        public MapDefinition Map { get; }
        public CardCatalog Cards { get; }
        public IReadOnlyDictionary<CardId,InfluenceEffectDefinition> Effects { get; }
        public PracticeInfluenceCatalog(MapDefinition map,CardCatalog cards,IEnumerable<InfluenceEffectDefinition> effects)
        {
            if(map==null || cards==null || effects==null)throw new ArgumentNullException();
            var copy=effects.Take(cards.Cards.Count+1).ToArray();
            if(copy.Length==0 || copy.Length>cards.Cards.Count || copy.Any(e=>e==null) || copy.Select(e=>e.CardId).Distinct(StringComparer.Ordinal).Count()!=copy.Length)throw new ArgumentException("Invalid practice effect catalog.");
            foreach(var effect in copy)
            {
                var printed=cards.GetCard(new CardId(effect.CardId));
                if(printed.Kind!=CardKind.Event || printed.Effect.EffectId!=effect.EffectId || printed.DisplayPersistentReminder
                    || printed.Affiliation!=(effect.Beneficiary==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR))throw new ArgumentException("Effect does not match the printed pure influence event.");
                bool Region(string id)=>map.GetCountriesInRegion(id).Count>0;
                var valid=effect switch {
                    MatchOpponentInfluenceEffect e=>e.Countries.All(map.Contains),
                    ClearThenControlEffect e=>map.Contains(e.Country),
                    AddThenRemoveFractionEffect e=>map.Contains(e.Country),
                    RemoveFromUncontrolledEffect e=>Region(e.Region),
                    AddToDistinctCountriesEffect e=>e.Regions.All(Region),
                    BudgetedRemovalEffect e=>e.Region!=null?Region(e.Region):e.Countries.All(map.Contains),
                    EraCountryRemovalEffect e=>Region(e.Region),
                    _=>false};
                if(!valid)throw new ArgumentException("Unsupported effect or invalid map reference.");
            }
            Map=map;Cards=cards;Effects=new ReadOnlyDictionary<CardId,InfluenceEffectDefinition>(copy.ToDictionary(e=>new CardId(e.CardId)));
        }
    }
}
