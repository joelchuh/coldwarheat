using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    public abstract class InfluenceEffectDefinition
    {
        public string CardId { get; }
        public string EffectId { get; }
        public PlayerSide Beneficiary { get; }
        protected InfluenceEffectDefinition(string cardId, string effectId, PlayerSide beneficiary)
        {
            if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Missing effect identity.");
            if (beneficiary != PlayerSide.US && beneficiary != PlayerSide.USSR) throw new ArgumentOutOfRangeException(nameof(beneficiary));
            CardId = cardId; EffectId = effectId; Beneficiary = beneficiary;
        }
    }

    public sealed class ClearThenControlEffect : InfluenceEffectDefinition
    {
        public CountryId Country { get; }
        public ClearThenControlEffect(string card, string effect, PlayerSide beneficiary, CountryId country) : base(card, effect, beneficiary) { Country = country; }
    }

    public sealed class AddThenRemoveFractionEffect : InfluenceEffectDefinition
    {
        public CountryId Country { get; }
        public int AddAmount { get; }
        public int Divisor { get; }
        public AddThenRemoveFractionEffect(string card, string effect, PlayerSide beneficiary, CountryId country, int addAmount, int divisor) : base(card, effect, beneficiary)
        {
            if (addAmount < 0 || divisor < 1) throw new ArgumentOutOfRangeException();
            Country = country; AddAmount = addAmount; Divisor = divisor;
        }
    }

    public sealed class RemoveFromUncontrolledEffect : InfluenceEffectDefinition
    {
        public string Region { get; }
        public RemoveFromUncontrolledEffect(string card, string effect, PlayerSide beneficiary, string region) : base(card, effect, beneficiary)
        {
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Missing region.");
            Region = region;
        }
    }

    public enum InfluenceControlFilter { Any, NotOpponentControlled }
    public sealed class AddToDistinctCountriesEffect : InfluenceEffectDefinition
    {
        public IReadOnlyList<string> Regions { get; }
        public int TargetCount { get; }
        public int Amount { get; }
        public InfluenceControlFilter ControlFilter { get; }
        public AddToDistinctCountriesEffect(string card, string effect, PlayerSide beneficiary, IEnumerable<string> regions, int targetCount, int amount, InfluenceControlFilter controlFilter=InfluenceControlFilter.Any) : base(card, effect, beneficiary)
        {
            var copy = regions?.ToArray();
            if (copy == null || copy.Length == 0 || copy.Any(string.IsNullOrWhiteSpace) || copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("Invalid regions.");
            if (targetCount < 1 || targetCount > InfluenceEventSelection.MaxTargets || amount < 1) throw new ArgumentOutOfRangeException();
            if (controlFilter != InfluenceControlFilter.Any && controlFilter != InfluenceControlFilter.NotOpponentControlled) throw new ArgumentOutOfRangeException(nameof(controlFilter));
            Regions = Array.AsReadOnly(copy); TargetCount = targetCount; Amount = amount; ControlFilter=controlFilter;
        }
    }
}
