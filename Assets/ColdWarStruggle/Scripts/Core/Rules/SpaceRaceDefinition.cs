using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum SpaceAbilityKind { AttemptLimit, HeadlineSelectionPolicy, EndTurnDiscardMaximum, ActionRoundAllowance }
    internal static class SpaceDefinitionGuard
    {
        internal static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value != value.Trim())
                throw new ArgumentException("Space identifier must be nonempty, trimmed and bounded.");
            return value;
        }
    }
    public sealed class SpaceAbilityDefinition
    {
        public string Id { get; }
        public SpaceAbilityKind Kind { get; }
        public int? Value { get; }
        public string HeadlinePolicy { get; }
        public SpaceAbilityDefinition(string id, SpaceAbilityKind kind, int? value = null, string headlinePolicy = null)
        {
            Id = SpaceDefinitionGuard.Id(id);
            if (!Enum.IsDefined(typeof(SpaceAbilityKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == SpaceAbilityKind.HeadlineSelectionPolicy)
            {
                if (value.HasValue || headlinePolicy != "OpponentRevealsFirst") throw new ArgumentException("Unsupported headline ability.");
            }
            else if (!value.HasValue || value.Value <= 0 || headlinePolicy != null) throw new ArgumentException("Positive numeric ability required.");
            Kind = kind; Value = value; HeadlinePolicy = headlinePolicy;
        }
    }
    public sealed class SpaceStepDefinition
    {
        public string Id { get; }
        public int Position { get; }
        public int MinimumEffectiveOps { get; }
        public int SuccessMinimum { get; }
        public int SuccessMaximum { get; }
        public int FirstVp { get; }
        public int SecondVp { get; }
        public string AbilityId { get; }
        public SpaceStepDefinition(string id, int position, int minimumOps, int successMin, int successMax, int firstVp, int secondVp, string abilityId = null)
        {
            Id = SpaceDefinitionGuard.Id(id);
            if (position <= 0 || minimumOps <= 0 || successMin <= 0 || successMax < successMin || firstVp < 0 || secondVp < 0)
                throw new ArgumentException("Invalid space step parameters.");
            if (abilityId != null) SpaceDefinitionGuard.Id(abilityId);
            Position = position; MinimumEffectiveOps = minimumOps; SuccessMinimum = successMin; SuccessMaximum = successMax;
            FirstVp = firstVp; SecondVp = secondVp; AbilityId = abilityId;
        }
    }
    public sealed class SpaceRaceDefinition
    {
        // Engineering limits; the ordinary game profile is validated by its loader.
        const int MaximumEntries = 64;
        public int InitialPosition { get; }
        public int FinalPosition { get; }
        public int BaseAttemptsPerTurn { get; }
        public int DieMinimum { get; }
        public int DieMaximum { get; }
        public IReadOnlyList<SpaceStepDefinition> Spaces { get; }
        public IReadOnlyList<SpaceAbilityDefinition> Abilities { get; }
        public SpaceRaceDefinition(int initialPosition, int baseAttempts, int dieMinimum, int dieMaximum, IEnumerable<SpaceStepDefinition> spaces, IEnumerable<SpaceAbilityDefinition> abilities)
        {
            if (initialPosition < 0 || baseAttempts <= 0 || dieMinimum <= 0 || dieMaximum < dieMinimum) throw new ArgumentException("Invalid space track parameters.");
            if (spaces == null || abilities == null) throw new ArgumentNullException(spaces == null ? nameof(spaces) : nameof(abilities));
            var steps = spaces.Take(MaximumEntries + 1).ToArray();
            var rewards = abilities.Take(MaximumEntries + 1).ToArray();
            if (steps.Length == 0 || steps.Length > MaximumEntries || rewards.Length > MaximumEntries || steps.Any(s => s == null) || rewards.Any(a => a == null))
                throw new ArgumentException("Space entries missing or outside engineering limits.");
            var stepIds = new HashSet<string>(StringComparer.Ordinal);
            var abilityIds = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<SpaceAbilityKind>();
            foreach (var ability in rewards)
            {
                if (!abilityIds.Add(ability.Id) || !kinds.Add(ability.Kind)) throw new ArgumentException("Duplicate space ability id or kind.");
                if (ability.Kind == SpaceAbilityKind.AttemptLimit && ability.Value.Value < baseAttempts) throw new ArgumentException("Attempt ability cannot reduce base allowance.");
            }
            var referenced = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (step.Position != (long)initialPosition + i + 1 || !stepIds.Add(step.Id) || step.SuccessMinimum < dieMinimum || step.SuccessMaximum > dieMaximum)
                    throw new ArgumentException("Space sequence, identifier or die range invalid.");
                if (step.AbilityId != null && (!abilityIds.Contains(step.AbilityId) || !referenced.Add(step.AbilityId)))
                    throw new ArgumentException("Unknown or multiply referenced space ability.");
            }
            if (!referenced.SetEquals(abilityIds)) throw new ArgumentException("Unreferenced space ability.");
            InitialPosition = initialPosition; FinalPosition = steps[steps.Length - 1].Position;
            BaseAttemptsPerTurn = baseAttempts; DieMinimum = dieMinimum; DieMaximum = dieMaximum;
            Spaces = Array.AsReadOnly(steps); Abilities = Array.AsReadOnly(rewards);
        }
        public SpaceStepDefinition GetSpace(int position)
        {
            if (position <= InitialPosition || position > FinalPosition) throw new ArgumentOutOfRangeException(nameof(position));
            return Spaces[position - InitialPosition - 1];
        }
    }
}
