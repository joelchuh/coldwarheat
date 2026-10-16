using System;
namespace ColdWarStruggle.Core
{
    public enum SpaceEventEligibility { Allowed, TerminalRequiresRuling }
    public sealed class SpaceEventRequest
    {
        public string Id { get; }
        public string SourceActionId { get; }
        public int TurnNumber { get; }
        public PlayerSide Beneficiary { get; }
        public SpaceEventDefinition Definition { get; }
        public SpaceEventRequest(string id,string sourceActionId,int turnNumber,PlayerSide beneficiary,SpaceEventDefinition definition)
        {
            SpaceDefinitionGuard.Id(id);SpaceDefinitionGuard.Id(sourceActionId);
            if(turnNumber<1 || (beneficiary!=PlayerSide.US && beneficiary!=PlayerSide.USSR))throw new ArgumentException("Invalid space event facts.");
            Definition=definition??throw new ArgumentNullException(nameof(definition));
            Id=id;SourceActionId=sourceActionId;TurnNumber=turnNumber;Beneficiary=beneficiary;
        }
        internal bool SameFacts(SpaceEventRequest other)=>other!=null && Id==other.Id && SourceActionId==other.SourceActionId
            && TurnNumber==other.TurnNumber && Beneficiary==other.Beneficiary && ReferenceEquals(Definition,other.Definition);
    }
    public sealed class SpaceEventReceipt
    {
        public SpaceEventRequest Request { get; }
        public int FromPosition { get; }
        public int ToPosition { get; }
        public bool FirstArrival { get; }
        public int AwardVp { get; }
        public int NetVpForUS { get; }
        internal SpaceEventReceipt(SpaceEventRequest request,SpaceAdvanceCalculation advance)
        { Request=request;FromPosition=advance.FromPosition;ToPosition=advance.ToPosition;FirstArrival=advance.FirstArrival;AwardVp=advance.AwardVp;NetVpForUS=advance.NetVpForUS; }
    }
    public sealed class SpaceEventPlan
    {
        public SpaceAttemptState Before { get; }
        public SpaceAttemptState After { get; }
        public SpaceEventReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        public int VpDeltaToApply => AlreadyApplied ? 0 : Receipt.NetVpForUS;
        internal SpaceEventPlan(SpaceAttemptState before,SpaceAttemptState after,SpaceEventReceipt receipt,bool alreadyApplied)
        { Before=before;After=after;Receipt=receipt;AlreadyApplied=alreadyApplied; }
    }
}
