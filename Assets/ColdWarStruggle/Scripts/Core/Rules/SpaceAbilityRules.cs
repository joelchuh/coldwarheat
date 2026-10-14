using System;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public sealed class SpaceAbilitySnapshot
    {
        public PlayerSide Owner { get; }
        public int AttemptLimit { get; }
        public bool OpponentRevealsHeadlineFirst { get; }
        public int EndTurnDiscardMaximum { get; }
        public int? ActionRoundAllowance { get; }
        public IReadOnlyList<string> ActiveAbilityIds { get; }
        internal SpaceAbilitySnapshot(PlayerSide owner, int attempts, bool headline, int discard, int? rounds, string[] ids)
        { Owner = owner; AttemptLimit = attempts; OpponentRevealsHeadlineFirst = headline; EndTurnDiscardMaximum = discard; ActionRoundAllowance = rounds; ActiveAbilityIds = Array.AsReadOnly(ids); }
    }
    public static class SpaceAbilityRules
    {
        // Values describe current entitlements, not phase eligibility or authorization to act.
        public static SpaceAbilitySnapshot Query(SpaceTrackState state, PlayerSide side)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var own = state.PositionFor(side);
            var other = state.PositionFor(side == PlayerSide.US ? PlayerSide.USSR : PlayerSide.US);
            var attempts = state.Definition.BaseAttemptsPerTurn;
            var headline = false; var discard = 0; int? rounds = null;
            var ids = new List<string>();
            foreach (var step in state.Definition.Spaces)
            {
                if (step.AbilityId == null || own < step.Position || other >= step.Position) continue;
                // Definitions guarantee a unique matching ability for every milestone.
                var ability = System.Linq.Enumerable.Single(state.Definition.Abilities, a => a.Id == step.AbilityId);
                ids.Add(ability.Id);
                switch (ability.Kind)
                {
                    case SpaceAbilityKind.AttemptLimit: attempts = ability.Value.Value; break;
                    case SpaceAbilityKind.HeadlineSelectionPolicy: headline = true; break;
                    case SpaceAbilityKind.EndTurnDiscardMaximum: discard = ability.Value.Value; break;
                    case SpaceAbilityKind.ActionRoundAllowance: rounds = ability.Value.Value; break;
                    default: throw new InvalidOperationException("Unrecognized space ability.");
                }
            }
            return new SpaceAbilitySnapshot(side, attempts, headline, discard, rounds, ids.ToArray());
        }
    }
}
