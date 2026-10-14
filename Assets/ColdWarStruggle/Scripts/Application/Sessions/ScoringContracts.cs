using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicRegionScore
    {
        public string RegionId { get; }
        public RegionSideScore US { get; }
        public RegionSideScore USSR { get; }
        public int? NetVpForUS { get; }
        public PlayerSide? EuropeControlWinner { get; }
        internal PublicRegionScore(RegionScoringResult score)
        {RegionId=score.RegionId;US=score.US;USSR=score.USSR;NetVpForUS=score.NetVpForUS;EuropeControlWinner=score.EuropeControlWinner;}
    }
    public sealed class ScoringPreviewResult
    {
        public string GameId { get; }
        public long Version { get; }
        public RuleError Error { get; }
        public PublicRegionScore Score { get; }
        internal ScoringPreviewResult(GameState state,RuleError error,RegionScoringResult score=null)
        {GameId=state.GameId;Version=state.Version;Error=error;Score=score==null?null:new PublicRegionScore(score);}
    }
}
