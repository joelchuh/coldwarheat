using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    // Exactly one target scope. Definitions are supplied by trusted data loading, never a player command.
    public sealed class BudgetedRemovalEffect : InfluenceEffectDefinition
    {
        public int TotalAmount { get; }
        public int PerCountryCap { get; }
        public string Region { get; }
        public IReadOnlyList<CountryId> Countries { get; }
        public string BlockedByEffectId { get; }
        public BudgetedRemovalEffect(string card, string effect, PlayerSide beneficiary, int totalAmount,
            int perCountryCap, string region = null, IEnumerable<CountryId> countries = null, string blockedByEffectId = null)
            : base(card, effect, beneficiary)
        {
            if (totalAmount < 1 || perCountryCap < 1) throw new ArgumentOutOfRangeException();
            if ((region == null) == (countries == null) || (region != null && string.IsNullOrWhiteSpace(region)))
                throw new ArgumentException("Exactly one nonempty target scope is required.");
            var copy = countries?.Take(InfluenceEventSelection.MaxTargets + 1).ToArray();
            if (copy != null && (copy.Length == 0 || copy.Length > InfluenceEventSelection.MaxTargets || copy.Distinct().Count() != copy.Length))
                throw new ArgumentException("Invalid target countries.");
            TotalAmount = totalAmount; PerCountryCap = perCountryCap; Region = region;
            Countries = copy == null ? null : Array.AsReadOnly(copy);
            BlockedByEffectId = blockedByEffectId;
        }
    }

    public enum InfluenceDecisionKind { None, CountrySet, Allocation }

    public readonly struct InfluenceAllocation
    {
        public CountryId Country { get; }
        public int Amount { get; }
        public InfluenceAllocation(CountryId country, int amount) { Country = country; Amount = amount; }
    }
}
