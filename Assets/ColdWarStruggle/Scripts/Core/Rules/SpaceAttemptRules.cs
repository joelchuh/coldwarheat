using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class SpaceAttemptPlan
    {
        public SpaceAttemptState Before { get; }
        public SpaceAttemptState After { get; }
        public SpaceAttemptReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        public int VpDeltaToApply => AlreadyApplied ? 0 : Receipt.NetVpForUS;
        public bool ApplyCardDisposition => !AlreadyApplied;
        public RandomSnapshot RandomBefore { get; }
        public RandomSnapshot RandomAfter { get; }
        internal SpaceAttemptPlan(SpaceAttemptState before, SpaceAttemptState after, SpaceAttemptReceipt receipt, bool alreadyApplied, RandomSnapshot randomBefore, RandomSnapshot randomAfter)
        { Before=before;After=after;Receipt=receipt;AlreadyApplied=alreadyApplied;RandomBefore=randomBefore;RandomAfter=randomAfter; }
    }
    public static class SpaceAttemptRules
    {
        // Trusted arithmetic plan only. Commit its state, RNG, card disposition, VP and cursor together in the host.
        public static SpaceAttemptPlan PlanAttempt(SpaceAttemptState state, SpaceAttemptRequest request, IRandomProvider random)
        {
            if (state == null || request == null || random == null) throw new ArgumentNullException();
            if (request.TurnNumber != state.Turn.Number) throw new ArgumentException("Space attempt belongs to a different turn.");
            if (state.EventReceipts.ContainsKey(request.Id)) throw new ArgumentException("Space ID already belongs to an event.");
            if (state.Receipts.TryGetValue(request.Id, out var prior))
            {
                if (!prior.Request.SameFacts(request)) throw new ArgumentException("Conflicting space attempt ID.");
                var current = random.Capture() ?? throw new InvalidOperationException("Missing random snapshot.");
                // Keep the current stream, not the old receipt's stream; later actions may have used it.
                return new SpaceAttemptPlan(state, state, prior, true, current, current);
            }
            var eligibility = ValidateAttempt(state, request);
            if (eligibility != SpaceAttemptEligibility.Allowed) throw new InvalidOperationException("Space attempt rejected: " + eligibility);
            if (state.ReceiptCount >= SpaceAttemptState.MaximumReceipts) throw new InvalidOperationException("Space receipt engineering limit reached.");
            var definition = state.Track.Definition;
            var from = state.Track.PositionFor(request.Actor);
            var target = definition.GetSpace(from + 1);
            var randomBefore = random.Capture() ?? throw new InvalidOperationException("Missing random snapshot.");
            var working = random.Fork();
            if (working == null || ReferenceEquals(working, random) || !SameSnapshot(randomBefore, working.Capture()))
                throw new InvalidOperationException("Independent random fork must start from the current snapshot.");
            // Shift the inclusive die range so even an int.MaxValue upper face has a representable exclusive bound.
            var sample = working.NextInt(definition.DieMinimum - 1, definition.DieMaximum);
            if (sample < definition.DieMinimum - 1 || sample >= definition.DieMaximum) throw new InvalidOperationException("Random provider returned an out-of-range die sample.");
            var roll = sample + 1;
            var randomAfter = working.Capture();
            if (randomAfter == null || randomAfter.AlgorithmId != randomBefore.AlgorithmId || randomAfter.Increment != randomBefore.Increment || randomAfter.DrawCount <= randomBefore.DrawCount)
                throw new InvalidOperationException("Random provider did not produce a valid advanced snapshot.");
            var success = roll >= target.SuccessMinimum && roll <= target.SuccessMaximum;
            var advance = success ? SpaceAdvanceRules.CalculateSingleStep(state.Track, request.Actor) : null;
            bool? first = advance?.FirstArrival;
            var award = advance?.AwardVp ?? 0;
            var to = success ? target.Position : from;
            var receipt = new SpaceAttemptReceipt(request, roll, from, to, first, award, randomBefore, randomAfter);
            var usAttempts = state.UsAttempts; var ussrAttempts = state.UssrAttempts;
            if (request.Actor == PlayerSide.US) usAttempts = checked(usAttempts + 1);
            else ussrAttempts = checked(ussrAttempts + 1);
            var track = advance?.After ?? state.Track;
            var receipts = state.Receipts.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            receipts.Add(request.Id, receipt);
            var after = new SpaceAttemptState(track, state.Calendar, state.Turn, usAttempts, ussrAttempts, receipts, state.EventReceipts);
            return new SpaceAttemptPlan(state, after, receipt, false, randomBefore, randomAfter);
        }
        static bool SameSnapshot(RandomSnapshot a, RandomSnapshot b) => a != null && b != null
            && a.AlgorithmId == b.AlgorithmId && a.State == b.State && a.Increment == b.Increment && a.DrawCount == b.DrawCount;
        // Eligibility within the arithmetic subset; ownership, phase, restrictions and hooks are host responsibilities.
        public static SpaceAttemptEligibility ValidateAttempt(SpaceAttemptState state, SpaceAttemptRequest request)
        {
            if (state == null || request == null) throw new ArgumentNullException();
            if (request.TurnNumber != state.Turn.Number) throw new ArgumentException("Space attempt belongs to a different turn.");
            if (request.Card.Kind == CardKind.Scoring) return SpaceAttemptEligibility.ScoringCard;
            var position = state.Track.PositionFor(request.Actor);
            if (position == state.Track.Definition.FinalPosition) return SpaceAttemptEligibility.TrackCompleted;
            if (state.AttemptsFor(request.Actor) >= SpaceAbilityRules.Query(state.Track, request.Actor).AttemptLimit)
                return SpaceAttemptEligibility.AttemptLimitReached;
            return request.EffectiveOps < state.Track.Definition.GetSpace(position + 1).MinimumEffectiveOps
                ? SpaceAttemptEligibility.InsufficientOps : SpaceAttemptEligibility.Allowed;
        }
        // The host must call only at TurnStartBeforeHeadline; repeat calls never erase spent attempts.
        public static SpaceAttemptState BeginNextTurn(SpaceAttemptState state, int nextTurnNumber)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (nextTurnNumber == state.Turn.Number) return state;
            if ((long)nextTurnNumber != (long)state.Turn.Number + 1) throw new ArgumentException("Only the next turn may reset space attempts.");
            var next = state.Calendar.ForTurn(nextTurnNumber);
            return new SpaceAttemptState(state.Track, state.Calendar, next, 0, 0, state.Receipts, state.EventReceipts);
        }
    }
}
