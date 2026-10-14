using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceAbilityTests
    {
        internal static List<SpaceAbilityDefinition> Abilities(int attempts = 2, int discard = 1, int rounds = 8) => new List<SpaceAbilityDefinition>
        {
            new SpaceAbilityDefinition("a.attempts", SpaceAbilityKind.AttemptLimit, attempts),
            new SpaceAbilityDefinition("a.headline", SpaceAbilityKind.HeadlineSelectionPolicy, headlinePolicy: "OpponentRevealsFirst"),
            new SpaceAbilityDefinition("a.discard", SpaceAbilityKind.EndTurnDiscardMaximum, discard),
            new SpaceAbilityDefinition("a.rounds", SpaceAbilityKind.ActionRoundAllowance, rounds)
        };
        internal static List<SpaceStepDefinition> Steps() => Enumerable.Range(1, 8).Select(p => new SpaceStepDefinition(
            "fixture.space." + p, p, p < 5 ? 2 : p < 8 ? 3 : 4, 1, p == 8 ? 2 : p % 2 == 0 ? 4 : 3,
            p == 1 || p == 3 || p == 8 ? 2 : p == 5 ? 3 : p == 7 ? 4 : 0, p == 1 || p == 5 ? 1 : p == 7 ? 2 : 0,
            p == 2 ? "a.attempts" : p == 4 ? "a.headline" : p == 6 ? "a.discard" : p == 8 ? "a.rounds" : null)).ToList();
        static SpaceRaceDefinition Definition() => new SpaceRaceDefinition(0, 1, 1, 6, Steps(), Abilities());
        static IEnumerable Positions()
        {
            for (var us = 0; us <= 8; us++) for (var ussr = 0; ussr <= 8; ussr++)
                yield return new TestCaseData(us, ussr).SetName("Both_sides_abilities_at_" + us + "_" + ussr);
        }
        [TestCaseSource(nameof(Positions))]
        public void Milestone_ownership_depends_on_both_markers(int us, int ussr)
        {
            var state = new SpaceTrackState(Definition(), us, ussr);
            foreach (var side in new[] { PlayerSide.US, PlayerSide.USSR })
            {
                var own = side == PlayerSide.US ? us : ussr;
                var other = side == PlayerSide.US ? ussr : us;
                var result = SpaceAbilityRules.Query(state, side);
                Assert.That(result.Owner, Is.EqualTo(side));
                Assert.That(state.PositionFor(side), Is.EqualTo(own));
                Assert.That(result.AttemptLimit, Is.EqualTo(own >= 2 && other < 2 ? 2 : 1));
                Assert.That(result.OpponentRevealsHeadlineFirst, Is.EqualTo(own >= 4 && other < 4));
                Assert.That(result.EndTurnDiscardMaximum, Is.EqualTo(own >= 6 && other < 6 ? 1 : 0));
                Assert.That(result.ActionRoundAllowance, Is.EqualTo(own >= 8 && other < 8 ? (int?)8 : null));
                Assert.That(result.ActiveAbilityIds.Count, Is.EqualTo(new[] {2,4,6,8}.Count(p => own >= p && other < p)));
            }
            Assert.That(state.US, Is.EqualTo(us)); Assert.That(state.USSR, Is.EqualTo(ussr));
        }
        [Test]
        public void Values_are_data_driven_and_collections_cannot_be_changed()
        {
            var steps = Steps(); var abilities = Abilities(3, 2, 9);
            var definition = new SpaceRaceDefinition(0, 1, 1, 6, steps, abilities);
            steps.Clear(); abilities.Clear();
            var result = SpaceAbilityRules.Query(new SpaceTrackState(definition, 8, 0), PlayerSide.US);
            Assert.That(result.AttemptLimit, Is.EqualTo(3)); Assert.That(result.EndTurnDiscardMaximum, Is.EqualTo(2));
            Assert.That(result.ActionRoundAllowance, Is.EqualTo(9)); Assert.That(result.ActiveAbilityIds.Count, Is.EqualTo(4));
            Assert.That(definition.Spaces.Count, Is.EqualTo(8)); Assert.That(definition.GetSpace(5).FirstVp, Is.EqualTo(3));
            Assert.Throws<NotSupportedException>(() => ((IList)definition.Spaces).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)definition.Abilities).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)result.ActiveAbilityIds).Clear());
        }
        [Test]
        public void Synthetic_start_position_and_base_are_respected()
        {
            var shifted = Steps().Select(s => new SpaceStepDefinition(s.Id, s.Position + 3, s.MinimumEffectiveOps, s.SuccessMinimum, s.SuccessMaximum, s.FirstVp, s.SecondVp, s.AbilityId));
            var definition = new SpaceRaceDefinition(3, 2, 1, 6, shifted, Abilities(3));
            Assert.That(definition.FinalPosition, Is.EqualTo(11));
            Assert.That(SpaceAbilityRules.Query(new SpaceTrackState(definition, 3, 3), PlayerSide.US).AttemptLimit, Is.EqualTo(2));
            Assert.That(definition.GetSpace(4).Id, Is.EqualTo("fixture.space.1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.GetSpace(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.GetSpace(12));
        }
        [TestCase(-1,0)] [TestCase(0,-1)] [TestCase(9,0)] [TestCase(0,9)]
        public void Invalid_positions_rejected(int us, int ussr) => Assert.That(() => new SpaceTrackState(Definition(), us, ussr), Throws.InstanceOf<ArgumentException>());
        [Test]
        public void Null_and_invalid_sides_rejected()
        {
            Assert.Throws<ArgumentNullException>(() => new SpaceTrackState(null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => SpaceAbilityRules.Query(null, PlayerSide.US));
            var state = new SpaceTrackState(Definition(), 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.PositionFor((PlayerSide)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpaceAbilityRules.Query(state, (PlayerSide)99));
        }
        [TestCase("id")] [TestCase("kind")] [TestCase("missing")] [TestCase("zero")] [TestCase("numericPolicy")] [TestCase("headlineValue")] [TestCase("unknownPolicy")]
        public void Invalid_abilities_rejected(string defect)
        {
            var kind = defect == "kind" ? (SpaceAbilityKind)99 : defect == "headlineValue" || defect == "unknownPolicy" ? SpaceAbilityKind.HeadlineSelectionPolicy : SpaceAbilityKind.AttemptLimit;
            Assert.That(() => new SpaceAbilityDefinition(defect == "id" ? " " : "a", kind,
                defect == "missing" || defect == "unknownPolicy" ? null : (int?)(defect == "zero" ? 0 : 1),
                defect == "unknownPolicy" ? "Unknown" : defect == "numericPolicy" || defect == "headlineValue" ? "OpponentRevealsFirst" : null), Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("id")] [TestCase("position")] [TestCase("ops")] [TestCase("range")] [TestCase("first")] [TestCase("second")] [TestCase("ability")]
        public void Invalid_steps_rejected(string defect) => Assert.That(() => new SpaceStepDefinition(defect == "id" ? "" : "s", defect == "position" ? 0 : 1, defect == "ops" ? 0 : 2, 1, defect == "range" ? 0 : 3, defect == "first" ? -1 : 0, defect == "second" ? -1 : 0, defect == "ability" ? " " : null), Throws.InstanceOf<ArgumentException>());
        [TestCase("initial")] [TestCase("base")] [TestCase("dieMin")] [TestCase("dieRange")] [TestCase("nullSteps")] [TestCase("nullAbilities")] [TestCase("emptySteps")] [TestCase("duplicateStep")] [TestCase("gap")] [TestCase("stepNull")] [TestCase("dieOutside")] [TestCase("duplicateAbility")] [TestCase("sameKind")] [TestCase("missingAbility")] [TestCase("unusedAbility")] [TestCase("abilityTwice")] [TestCase("abilityNull")] [TestCase("tooMany")] [TestCase("lowerAttemptAbility")]
        public void Inconsistent_definitions_rejected(string defect)
        {
            var steps = Steps(); var abilities = Abilities();
            switch (defect)
            {
                case "nullSteps": steps = null; break; case "nullAbilities": abilities = null; break;
                case "emptySteps": steps.Clear(); break; case "duplicateStep": steps[1] = steps[0]; break;
                case "gap": steps.RemoveAt(0); break; case "stepNull": steps[0] = null; break;
                case "duplicateAbility": abilities.Add(abilities[0]); break;
                case "sameKind": abilities.Add(new SpaceAbilityDefinition("different", SpaceAbilityKind.AttemptLimit, 2)); break;
                case "missingAbility": abilities.RemoveAt(0); break;
                case "unusedAbility": steps[1] = new SpaceStepDefinition("s2", 2, 2, 1, 3, 0, 0); break;
                case "abilityTwice": steps[0] = new SpaceStepDefinition("s1", 1, 2, 1, 3, 0, 0, "a.attempts"); break;
                case "abilityNull": abilities[0] = null; break;
                case "tooMany": steps = Enumerable.Range(1, 65).Select(p => new SpaceStepDefinition("s"+p,p,1,1,1,0,0)).ToList(); abilities.Clear(); break;
            }
            Assert.That(() => new SpaceRaceDefinition(defect == "initial" ? -1 : 0, defect == "base" ? 0 : defect == "lowerAttemptAbility" ? 3 : 1, defect == "dieMin" ? 0 : 1, defect == "dieRange" ? 0 : defect == "dieOutside" ? 3 : 6, steps, abilities), Throws.InstanceOf<ArgumentException>());
        }
    }
}
