using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicSpaceEventResult
    {
        public CardId Card { get; }
        public PlayerSide Beneficiary { get; }
        public int FromPosition { get; }
        public int ToPosition { get; }
        public bool FirstArrival { get; }
        public int AwardVp { get; }
        public int NetVpForUS { get; }
        public SpaceAbilitySnapshot BeforeUS { get; }
        public SpaceAbilitySnapshot AfterUS { get; }
        public SpaceAbilitySnapshot BeforeUSSR { get; }
        public SpaceAbilitySnapshot AfterUSSR { get; }
        internal PublicSpaceEventResult(SpaceEventPlan plan)
        {
            var r=plan.Receipt;Card=r.Request.Definition.Card.Id;Beneficiary=r.Request.Beneficiary;
            FromPosition=r.FromPosition;ToPosition=r.ToPosition;FirstArrival=r.FirstArrival;AwardVp=r.AwardVp;NetVpForUS=r.NetVpForUS;
            BeforeUS=SpaceAbilityRules.Query(plan.Before.Track,PlayerSide.US);AfterUS=SpaceAbilityRules.Query(plan.After.Track,PlayerSide.US);
            BeforeUSSR=SpaceAbilityRules.Query(plan.Before.Track,PlayerSide.USSR);AfterUSSR=SpaceAbilityRules.Query(plan.After.Track,PlayerSide.USSR);
        }
    }
}
