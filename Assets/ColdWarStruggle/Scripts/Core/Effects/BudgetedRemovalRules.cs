using System;
using System.Linq;

namespace ColdWarStruggle.Core
{
    internal static class BudgetedRemovalRules
    {
        // General event guards (ended state, trusted interference policy) run before this query.
        internal static InfluenceEventQuery Query(GameState state, BudgetedRemovalEffect definition)
        {
            var countries = definition.Region == null ? definition.Countries.ToArray() :
                state.Map.GetCountriesInRegion(definition.Region).Select(c => c.Id).ToArray();
            if (countries.Length == 0 || countries.Length > InfluenceEventSelection.MaxTargets || countries.Any(c => !state.Map.Contains(c)))
                return new InfluenceEventQuery(InfluenceEventStatus.Rejected, InfluenceEventError.InvalidDefinition,
                    definition.Beneficiary, state.Version, 0, Array.Empty<CountryId>());
            var opponent = definition.Beneficiary == PlayerSide.US ? PlayerSide.USSR : PlayerSide.US;
            var capacities = countries.OrderBy(c => c.Value, StringComparer.Ordinal)
                .Select(c => new InfluenceAllocation(c, Math.Min(state.GetInfluence(c, opponent), definition.PerCountryCap)))
                .Where(c => c.Amount > 0).ToArray();
            var required = (int)Math.Min(definition.TotalAmount, capacities.Sum(c => (long)c.Amount));
            return new InfluenceEventQuery(definition.Beneficiary, state.Version, required, capacities);
        }

        internal static bool IsValid(InfluenceEventQuery query, InfluenceEventSelection selection)
        {
            var values = selection.Allocations;
            if (values == null || values.Count == 0 || values.Count > InfluenceEventSelection.MaxTargets
                || values.Select(v => v.Country).Distinct().Count() != values.Count) return false;
            var capacities = query.Capacities.ToDictionary(c => c.Country, c => c.Amount);
            return values.All(v => v.Amount > 0 && capacities.TryGetValue(v.Country, out var capacity) && v.Amount <= capacity)
                && values.Sum(v => (long)v.Amount) == query.RequiredAmount;
        }
    }
}
