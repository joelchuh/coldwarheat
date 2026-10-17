using System;
namespace ColdWarStruggle.Core
{
    public sealed class AddFixedInfluenceEffect : InfluenceEffectDefinition
    {
        public CountryId Country { get; }
        public int Amount { get; }
        public AddFixedInfluenceEffect(string card,string effect,PlayerSide beneficiary,CountryId country,int amount):base(card,effect,beneficiary)
        { if(string.IsNullOrWhiteSpace(country.Value) || amount<1)throw new ArgumentException("Invalid fixed influence grant.");Country=country;Amount=amount; }
    }
}
