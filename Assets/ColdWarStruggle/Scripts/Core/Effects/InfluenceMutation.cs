using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    // Private transaction buffer. No reference to these writable collections escapes a plan.
    internal sealed class InfluenceMutation
    {
        readonly GameState source;
        readonly Dictionary<CountryId, CountryState> countries;
        readonly List<InfluenceChanged> changes = new List<InfluenceChanged>();

        public InfluenceMutation(GameState source)
        {
            this.source = source;
            countries = source.Countries.ToDictionary(p => p.Key, p => p.Value);
        }

        int Get(CountryId country, PlayerSide side) => countries[country].For(side);
        void Set(CountryId country, PlayerSide side, int after)
        {
            if (after < 0) throw new ArgumentOutOfRangeException(nameof(after));
            var before = Get(country, side);
            if (before == after) return;
            var old = countries[country];
            var sequence = checked(source.EventSequence + changes.Count + 1);
            countries[country] = side == PlayerSide.US ? new CountryState(after, old.USSR) : new CountryState(old.US, after);
            changes.Add(new InfluenceChanged(country, side, before, after, sequence));
        }

        public void Add(CountryId country, PlayerSide side, int amount) => Set(country, side, checked(Get(country, side) + amount));
        public void RemoveAll(CountryId country, PlayerSide side) => Set(country, side, 0);
        public void RemoveFractionCeiling(CountryId country, PlayerSide side, int divisor)
        {
            int before = Get(country, side);
            int removed = before / divisor + (before % divisor == 0 ? 0 : 1);
            Set(country, side, before - removed);
        }
        public void EnsureControlByAdding(CountryId country, PlayerSide side)
        {
            var other = side == PlayerSide.US ? PlayerSide.USSR : PlayerSide.US;
            long needed = (long)Get(country, other) + source.Map.GetCountry(country).Stability;
            Set(country, side, checked((int)Math.Max(Get(country, side), needed)));
        }
        public InfluenceEventPlan Complete(InfluenceEffectDefinition definition)
        {
            // The final event is resolution itself, including a zero-change resolution.
            var next = source.Apply(countries, source.SpentOps, checked(changes.Count + 1));
            return new InfluenceEventPlan(changes.Count == 0 ? InfluenceEventStatus.ResolvedNoChange : InfluenceEventStatus.ResolvedChange,
                InfluenceEventError.None, definition, source, next, changes);
        }
    }
}
