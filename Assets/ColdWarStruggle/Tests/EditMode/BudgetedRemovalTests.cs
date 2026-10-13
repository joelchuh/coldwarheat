using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class BudgetedRemovalTests
    {
        public static CountryId Id(string name) => new CountryId("country." + name);
        public static GameState State(params (string country, int us, int ussr)[] values)
        {
            var map = DefinitionTests.World();
            var countries = map.Countries.ToDictionary(c => c.Id, c => new CountryState(0, 0));
            foreach (var value in values) countries[Id(value.country)] = new CountryState(value.us, value.ussr);
            return new GameState(map, countries, new OperationsContext("budget-fixture", PlayerSide.US, 0,
                Array.Empty<CountryId>(), costs: new InfluenceCosts(1, 2)));
        }
        public static BudgetedRemovalEffect Suez() => new BudgetedRemovalEffect("card.suez_crisis", "effect.suez_crisis",
            PlayerSide.USSR, 4, 2, countries: new[] { Id("france"), Id("uk"), Id("israel") });
        public static InfluenceAllocation A(string country, int amount) => new InfluenceAllocation(Id(country), amount);
        static InfluenceEventQuery Query(GameState state, BudgetedRemovalEffect effect = null)
            => InfluenceEventRules.Query(state, effect ?? Suez(), InfluenceEventPolicy.NoActiveInterference);
        static InfluenceEventPlan Plan(GameState state, IEnumerable<InfluenceAllocation> allocations, BudgetedRemovalEffect effect = null)
            => InfluenceEventRules.Plan(state, effect ?? Suez(), InfluenceEventSelection.Allocate(PlayerSide.USSR, state.Version, allocations), InfluenceEventPolicy.NoActiveInterference);

        [Test] public void Query_caps_each_country_and_requires_only_the_available_total()
        {
            var state = State(("france", 3, 9), ("uk", 1, 7), ("israel", 0, 4), ("egypt", 9, 0));
            var query = Query(state);
            Assert.That(query.Status, Is.EqualTo(InfluenceEventStatus.AwaitingChoice));
            Assert.That(query.Kind, Is.EqualTo(InfluenceDecisionKind.Allocation));
            Assert.That(query.RequiredTargets, Is.Zero); Assert.That(query.RequiredAmount, Is.EqualTo(3));
            Assert.That(query.Owner, Is.EqualTo(PlayerSide.USSR));
            Assert.That(query.LegalCountries, Is.EqualTo(new[] { Id("france"), Id("uk") }));
            Assert.That(query.Capacities.Select(c => c.Amount), Is.EqualTo(new[] { 2, 1 }));
            Assert.Throws<NotSupportedException>(() => ((IList<InfluenceAllocation>)query.Capacities).Clear());
        }

        [Test] public void Western_europe_includes_Austria_and_Finland_and_ignores_control_and_reach()
        {
            var state = State(("austria", 2, 8), ("finland", 2, 0), ("poland", 9, 0));
            var effect = new BudgetedRemovalEffect("card.socialist_governments", "effect.socialist_governments",
                PlayerSide.USSR, 3, 2, region: "region.western_europe", blockedByEffectId: "effect.the_iron_lady");
            var query = Query(state, effect);
            Assert.That(query.RequiredAmount, Is.EqualTo(3));
            Assert.That(query.LegalCountries, Is.EqualTo(new[] { Id("austria"), Id("finland") }));
            var plan = Plan(state, new[] { A("austria", 2), A("finland", 1) }, effect);
            Assert.That(plan.Resolved, Is.True); Assert.That(plan.NextState.GetInfluence(Id("austria"), PlayerSide.US), Is.Zero);
            Assert.That(plan.NextState.GetInfluence(Id("finland"), PlayerSide.US), Is.EqualTo(1));
            Assert.That(plan.NextState.GetInfluence(Id("poland"), PlayerSide.US), Is.EqualTo(9));
            Assert.That(plan.NextState.Operations, Is.SameAs(state.Operations)); Assert.That(plan.NextState.SpentOps, Is.Zero);
        }

        [Test] public void Shortage_removes_maximum_available_atomically_without_touching_beneficiary()
        {
            var state = State(("france", 3, 9), ("uk", 1, 7));
            var plan = Plan(state, new[] { A("uk", 1), A("france", 2) });
            Assert.That(plan.Status, Is.EqualTo(InfluenceEventStatus.ResolvedChange));
            Assert.That(plan.NextState.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(1));
            Assert.That(plan.NextState.GetInfluence(Id("uk"), PlayerSide.US), Is.Zero);
            Assert.That(plan.NextState.GetInfluence(Id("france"), PlayerSide.USSR), Is.EqualTo(9));
            Assert.That(plan.NextState.GetInfluence(Id("uk"), PlayerSide.USSR), Is.EqualTo(7));
            Assert.That(plan.Changes.Select(c => c.Country), Is.EqualTo(new[] { Id("france"), Id("uk") }));
            Assert.That(plan.Changes.Select(c => c.Sequence), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(plan.ResolutionSequence, Is.EqualTo(3)); Assert.That(plan.NextState.Version, Is.EqualTo(1));
            Assert.That(state.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(3)); Assert.That(state.Version, Is.Zero);
        }

        [Test] public void Allocation_can_span_three_countries_and_has_canonical_change_order()
        {
            var state = State(("france", 2, 0), ("uk", 2, 0), ("israel", 2, 0));
            var values = new[] { A("uk", 1), A("israel", 1), A("france", 2) };
            var first = Plan(state, values); var second = Plan(state, values.Reverse());
            Assert.That(first.Resolved, Is.True); Assert.That(first.Changes.Count, Is.EqualTo(3));
            Assert.That(first.NextState.GetInfluence(Id("uk"), PlayerSide.US), Is.EqualTo(1));
            Assert.That(first.NextState.GetInfluence(Id("israel"), PlayerSide.US), Is.EqualTo(1));
            Assert.That(first.Changes.Select(c => (c.Country, c.Before, c.After, c.Sequence)),
                Is.EqualTo(second.Changes.Select(c => (c.Country, c.Before, c.After, c.Sequence))));
        }

        [Test] public void Empty_capacity_completes_without_an_unanswerable_decision()
        {
            var state = State(("france", 0, 10)); var query = Query(state);
            Assert.That(query.Status, Is.EqualTo(InfluenceEventStatus.Ready)); Assert.That(query.RequiredAmount, Is.Zero);
            Assert.That(query.LegalCountries, Is.Empty);
            var plan = InfluenceEventRules.Plan(state, Suez(), null, InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Status, Is.EqualTo(InfluenceEventStatus.ResolvedNoChange)); Assert.That(plan.Changes, Is.Empty);
            Assert.That(plan.NextState.Version, Is.EqualTo(1)); Assert.That(plan.ResolutionSequence, Is.EqualTo(1));
            Assert.That(Plan(state, Array.Empty<InfluenceAllocation>()).Error, Is.EqualTo(InfluenceEventError.InvalidSelection));
        }

        [Test] public void Nonempty_capacity_waits_for_the_owner_without_mutating()
        {
            var state = State(("france", 3, 0));
            var plan = InfluenceEventRules.Plan(state, Suez(), null, InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Status, Is.EqualTo(InfluenceEventStatus.AwaitingChoice)); Assert.That(plan.NextState, Is.Null);
            Assert.That(state.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(3));
        }

        [TestCase("short")] [TestCase("excess")] [TestCase("cap")] [TestCase("negative")]
        [TestCase("zero")] [TestCase("duplicate")] [TestCase("outside")] [TestCase("unknown")]
        [TestCase("null")] [TestCase("empty")] [TestCase("oversized")] [TestCase("huge")]
        public void Invalid_allocation_rejects_the_entire_transaction(string kind)
        {
            var state = State(("france", 8, 5), ("uk", 2, 4), ("israel", 2, 3), ("egypt", 8, 0));
            InfluenceAllocation[] values = { A("france", 2), A("uk", 2) };
            switch (kind)
            {
                case "short": values = new[] { A("france", 2) }; break;
                case "excess": values = new[] { A("france", 2), A("uk", 2), A("israel", 1) }; break;
                case "cap": values = new[] { A("france", 3), A("uk", 1) }; break;
                case "negative": values = new[] { A("france", -1), A("uk", 2) }; break;
                case "zero": values = new[] { A("france", 2), A("uk", 2), A("israel", 0) }; break;
                case "duplicate": values = new[] { A("france", 2), A("france", 2) }; break;
                case "outside": values = new[] { A("france", 2), A("egypt", 2) }; break;
                case "unknown": values = new[] { A("france", 2), A("imaginary", 2) }; break;
                case "null": values = null; break;
                case "empty": values = Array.Empty<InfluenceAllocation>(); break;
                case "oversized": values = Enumerable.Repeat(A("france", 1), 129).ToArray(); break;
                case "huge": values = new[] { A("france", int.MaxValue), A("uk", int.MaxValue), A("israel", 6) }; break;
            }
            var plan = Plan(state, values);
            Assert.That(plan.Error, Is.EqualTo(InfluenceEventError.InvalidSelection));
            Assert.That(plan.NextState, Is.Null); Assert.That(plan.Changes, Is.Empty);
            Assert.That(state.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(8)); Assert.That(state.EventSequence, Is.Zero);
        }

        [TestCase("owner", InfluenceEventError.WrongDecisionOwner)]
        [TestCase("version", InfluenceEventError.StaleSelection)]
        [TestCase("kind", InfluenceEventError.InvalidSelection)]
        public void Allocation_enforces_identity_version_and_selection_kind(string kind, InfluenceEventError expected)
        {
            var state = State(("france", 2, 0));
            var selection = kind == "kind" ? new InfluenceEventSelection(PlayerSide.USSR, 0, new[] { Id("france") }) :
                InfluenceEventSelection.Allocate(kind == "owner" ? PlayerSide.US : PlayerSide.USSR, kind == "version" ? 1 : 0, new[] { A("france", 2) });
            Assert.That(InfluenceEventRules.Plan(state, Suez(), selection, InfluenceEventPolicy.NoActiveInterference).Error, Is.EqualTo(expected));
        }

        [Test] public void Allocation_is_not_accepted_by_a_country_set_event()
        {
            var state = State(); var effect = new AddToDistinctCountriesEffect("fixture", "fixture", PlayerSide.USSR, new[] { "region.africa" }, 1, 1);
            var plan = InfluenceEventRules.Plan(state, effect, InfluenceEventSelection.Allocate(PlayerSide.USSR, 0, new[] { A("angola", 1) }), InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Error, Is.EqualTo(InfluenceEventError.InvalidSelection)); Assert.That(plan.NextState, Is.Null);
        }

        [Test] public void Capacity_sum_exceeding_int_max_still_has_a_representable_budget()
        {
            var state = State(("france", int.MaxValue, 0), ("uk", int.MaxValue, 0));
            var effect = new BudgetedRemovalEffect("fixture", "fixture", PlayerSide.USSR, int.MaxValue, int.MaxValue,
                countries: new[] { Id("france"), Id("uk") });
            Assert.That(Query(state, effect).RequiredAmount, Is.EqualTo(int.MaxValue));
            var plan = Plan(state, new[] { A("france", int.MaxValue - 1), A("uk", 1) }, effect);
            Assert.That(plan.Resolved, Is.True); Assert.That(plan.NextState.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(1));
        }

        [TestCase(true)] [TestCase(false)] public void Completion_overflow_rejects_without_partial_changes(bool version)
        {
            var initial = State(("france", 2, 0));
            var state = new GameState(initial.Map, initial.Countries, initial.Operations,
                version: version ? long.MaxValue : 0, eventSequence: version ? 0 : long.MaxValue - 1);
            var plan = Plan(state, new[] { A("france", 2) });
            Assert.That(plan.Error, Is.EqualTo(InfluenceEventError.NumericOverflow)); Assert.That(plan.NextState, Is.Null);
            Assert.That(plan.Changes, Is.Empty); Assert.That(state.GetInfluence(Id("france"), PlayerSide.US), Is.EqualTo(2));
        }

        [Test] public void Selection_copies_input_before_it_can_be_changed()
        {
            var state = State(("france", 2, 0)); var values = new[] { A("france", 2) };
            var selection = InfluenceEventSelection.Allocate(PlayerSide.USSR, 0, values); values[0] = A("uk", 2);
            var plan = InfluenceEventRules.Plan(state, Suez(), selection, InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Resolved, Is.True); Assert.That(plan.NextState.GetInfluence(Id("france"), PlayerSide.US), Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList<InfluenceAllocation>)selection.Allocations).Clear());
        }

        [TestCase("none")] [TestCase("both")] [TestCase("empty")] [TestCase("duplicate")] [TestCase("oversized")]
        [TestCase("zeroTotal")] [TestCase("zeroCap")] [TestCase("blankRegion")]
        public void Malformed_definition_is_rejected_at_construction(string kind)
        {
            string region = null; CountryId[] countries = { Id("france") }; int total = 4, cap = 2;
            switch (kind)
            {
                case "none": countries = null; break;
                case "both": region = "region.europe"; break;
                case "empty": countries = Array.Empty<CountryId>(); break;
                case "duplicate": countries = new[] { Id("france"), Id("france") }; break;
                case "oversized": countries = Enumerable.Range(0, 129).Select(i => new CountryId("country.fixture" + i)).ToArray(); break;
                case "zeroTotal": total = 0; break;
                case "zeroCap": cap = 0; break;
                case "blankRegion": region = " "; countries = null; break;
            }
            Assert.Catch<ArgumentException>(() => new BudgetedRemovalEffect("fixture", "fixture", PlayerSide.USSR, total, cap, region, countries));
        }

        [TestCase(InfluenceEventPolicy.Unknown, InfluenceEventStatus.RequiresRuling)]
        [TestCase(InfluenceEventPolicy.Forbidden, InfluenceEventStatus.Blocked)]
        public void Unavailable_or_forbidden_interference_does_not_remove_influence(InfluenceEventPolicy policy, InfluenceEventStatus expected)
        {
            var state = State(("france", 4, 0)); var plan = InfluenceEventRules.Plan(state, Suez(), null, policy);
            Assert.That(plan.Status, Is.EqualTo(expected)); Assert.That(plan.NextState, Is.Null);
        }
    }
}
