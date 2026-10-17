using System;
namespace ColdWarStruggle.Core
{
    public enum TreatyAction { Influence, Coup, Realignment, EventAttack }
    public enum TreatyRestrictionStatus { NoTreatyBlock, BlockedByTreaty, InvalidContext }
    public sealed class TreatyRestrictionResult
    {
        public TreatyRestrictionStatus Status { get; }
        public RuleError Error { get; }
        public string EffectId { get; }
        internal TreatyRestrictionResult(TreatyRestrictionStatus status,RuleError error=RuleError.None,string effect=null)
        { Status=status;Error=error;EffectId=effect; }
    }
    public static class JapanPactRules
    {
        public static TreatyRestrictionResult Evaluate(GameState board,JapanPactState treaty,TreatyAction action,PlayerSide actor,CountryId country)
        {
            var error=treaty?.CheckBoard(board)??RuleError.InvalidDefinition;
            if(error==RuleError.None && ((actor!=PlayerSide.US && actor!=PlayerSide.USSR) || !Enum.IsDefined(typeof(TreatyAction),action)))error=RuleError.InvalidCommand;
            if(error==RuleError.None && !board.Map.Contains(country))error=RuleError.UnknownCountry;
            if(error!=RuleError.None)return new TreatyRestrictionResult(TreatyRestrictionStatus.InvalidContext,error);
            var blocked=treaty.Instance!=null && actor==PlayerSide.USSR && country.Equals(treaty.Definition.ImmediateInfluence.Country)
                && (action==TreatyAction.Coup || action==TreatyAction.Realignment);
            return blocked?new TreatyRestrictionResult(TreatyRestrictionStatus.BlockedByTreaty,RuleError.EventForbidden,treaty.Definition.EffectId)
                :new TreatyRestrictionResult(TreatyRestrictionStatus.NoTreatyBlock);
        }
    }
}
