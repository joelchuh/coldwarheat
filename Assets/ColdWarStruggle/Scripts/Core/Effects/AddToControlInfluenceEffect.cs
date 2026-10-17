using System;
namespace ColdWarStruggle.Core
{
    public sealed class AddToControlInfluenceEffect : InfluenceEffectDefinition
    {
        public CountryId Country { get; }
        public AddToControlInfluenceEffect(string card,string effect,PlayerSide side,CountryId country):base(card,effect,side)
        { if(string.IsNullOrWhiteSpace(country.Value))throw new ArgumentException("Missing control target.");Country=country; }
    }
}
