using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class RegionMetrics
    {
        public int Total { get; }
        public int Battlegrounds { get; }
        public int NonBattlegrounds => Total-Battlegrounds;
        public int EnemyAdjacent { get; }
        internal RegionMetrics(int total,int battlegrounds,int adjacent) { Total=total;Battlegrounds=battlegrounds;EnemyAdjacent=adjacent; }
    }
    public sealed class ActualRegionTiers
    {
        public string RegionId { get; }
        public string SnapshotId { get; }
        public long StateVersion { get; }
        public int TotalBattlegrounds { get; }
        public RegionMetrics US { get; }
        public RegionMetrics USSR { get; }
        public RegionTier UsTier { get; }
        public RegionTier UssrTier { get; }
        internal ActualRegionTiers(string region,ScoringSnapshot snapshot,int total,RegionMetrics us,RegionMetrics ussr,RegionTier usTier,RegionTier ussrTier)
        { RegionId=region;SnapshotId=snapshot.Id;StateVersion=snapshot.StateVersion;TotalBattlegrounds=total;US=us;USSR=ussr;UsTier=usTier;UssrTier=ussrTier; }
    }
    public sealed class RegionSideScore
    {
        public RegionTier? Tier { get; }
        public int? BaseVp { get; }
        public int BattlegroundVp { get; }
        public int EnemyAdjacencyVp { get; }
        public int WeightedVp { get; }
        public int? TotalVp { get; }
        internal RegionSideScore(RegionTier? tier,int? baseVp,int bg,int adjacent,int weighted,int? total)
        { Tier=tier;BaseVp=baseVp;BattlegroundVp=bg;EnemyAdjacencyVp=adjacent;WeightedVp=weighted;TotalVp=total; }
    }
    public sealed class RegionScoringResult
    {
        public RegionScoringDefinition Definition { get; }
        public string RegionId { get; }
        public string SnapshotId { get; }
        public long StateVersion { get; }
        public ActualRegionTiers Facts { get; }
        public RegionSideScore US { get; }
        public RegionSideScore USSR { get; }
        public int? NetVpForUS { get; }
        public PlayerSide? EuropeControlWinner { get; }
        internal RegionScoringResult(RegionScoringDefinition definition,string region,ScoringSnapshot snapshot,ActualRegionTiers facts,
            RegionSideScore us,RegionSideScore ussr,int? net,PlayerSide? europeWinner)
        { Definition=definition;RegionId=region;SnapshotId=snapshot.Id;StateVersion=snapshot.StateVersion;Facts=facts;US=us;USSR=ussr;NetVpForUS=net;EuropeControlWinner=europeWinner; }
    }
    public static class RegionScoringService
    {
        public static RegionScoringResult CalculateUnmodified(RegionScoringDefinition definition,ScoringSnapshot snapshot,string regionId)
        {
            Bind(definition,snapshot);
            if(snapshot.ActiveScoringEffects.Count!=0)throw new ArgumentException("Active scoring modifiers require their own verified handlers.");
            if(regionId==definition.WeightedRegionId)
            {
                var us=Weighted(definition,snapshot,PlayerSide.US);var ussr=Weighted(definition,snapshot,PlayerSide.USSR);
                return new RegionScoringResult(definition,regionId,snapshot,null,us,ussr,checked(us.TotalVp.Value-ussr.TotalVp.Value),null);
            }
            var table=definition.Regions.SingleOrDefault(t=>t.Id==regionId)??throw new ArgumentException("Unknown standard scoring region.");
            var facts=Facts(definition,snapshot,table.Id);
            PlayerSide? winner=table.Control.HasValue?null:facts.UsTier==RegionTier.Control?PlayerSide.US:facts.UssrTier==RegionTier.Control?PlayerSide.USSR:(PlayerSide?)null;
            // Automatic control is not a numeric score. All numeric columns remain uncomputed in this branch.
            var usScore=winner.HasValue?new RegionSideScore(facts.UsTier,null,0,0,0,null):Standard(definition,table,facts.US,facts.UsTier);
            var sovScore=winner.HasValue?new RegionSideScore(facts.UssrTier,null,0,0,0,null):Standard(definition,table,facts.USSR,facts.UssrTier);
            int? net=winner.HasValue?null:checked(usScore.TotalVp.Value-sovScore.TotalVp.Value);
            return new RegionScoringResult(definition,regionId,snapshot,facts,usScore,sovScore,net,winner);
        }
        public static IReadOnlyList<ActualRegionTiers> InspectActualRegionTiers(RegionScoringDefinition definition,ScoringSnapshot snapshot)
        {
            Bind(definition,snapshot);
            return Array.AsReadOnly(definition.Regions.Select(t=>Facts(definition,snapshot,t.Id)).ToArray());
        }
        static void Bind(RegionScoringDefinition definition,ScoringSnapshot snapshot)
        {
            if(definition==null || snapshot==null)throw new ArgumentNullException();
            if(!ReferenceEquals(definition.Map,snapshot.Map))throw new ArgumentException("Scoring map binding mismatch.");
        }
        static PlayerSide? Controller(ScoringSnapshot snapshot,CountryDefinition c)
        {
            var state=snapshot.Countries[c.Id];return ControlRules.GetController(state.US,state.USSR,c.Stability);
        }
        static ActualRegionTiers Facts(RegionScoringDefinition definition,ScoringSnapshot snapshot,string region)
        {
            var countries=definition.Map.GetCountriesInRegion(region);int total=countries.Count(c=>c.Battleground);
            var us=Metrics(countries,snapshot,PlayerSide.US);var ussr=Metrics(countries,snapshot,PlayerSide.USSR);
            return new ActualRegionTiers(region,snapshot,total,us,ussr,Tier(us,ussr,total),Tier(ussr,us,total));
        }
        static RegionMetrics Metrics(IEnumerable<CountryDefinition> countries,ScoringSnapshot snapshot,PlayerSide side)
        {
            var controlled=countries.Where(c=>Controller(snapshot,c)==side).ToArray();
            var adjacent=snapshot.Map.GetSuperpowerNeighbors(side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);
            return new RegionMetrics(controlled.Length,controlled.Count(c=>c.Battleground),controlled.Count(c=>adjacent.Contains(c.Id)));
        }
        static RegionTier Tier(RegionMetrics own,RegionMetrics other,int total)
        {
            if(own.Total>other.Total && own.Battlegrounds==total)return RegionTier.Control;
            if(own.Total>other.Total && own.Battlegrounds>other.Battlegrounds && own.Battlegrounds>=1 && own.NonBattlegrounds>=1)return RegionTier.Domination;
            return own.Total>0?RegionTier.Presence:RegionTier.None;
        }
        static RegionSideScore Standard(RegionScoringDefinition d,RegionScoreTable table,RegionMetrics metrics,RegionTier tier)
        {
            int baseVp=tier==RegionTier.Control?table.Control.Value:tier==RegionTier.Domination?table.Domination:tier==RegionTier.Presence?table.Presence:0;
            int bg=checked(metrics.Battlegrounds*d.BattlegroundBonus),adjacent=checked(metrics.EnemyAdjacent*d.EnemyAdjacencyBonus);
            return new RegionSideScore(tier,baseVp,bg,adjacent,0,checked(baseVp+bg+adjacent));
        }
        static RegionSideScore Weighted(RegionScoringDefinition d,ScoringSnapshot snapshot,PlayerSide side)
        {
            int total=0;
            foreach(var weight in d.CountryWeights)
                if(Controller(snapshot,d.Map.GetCountry(weight.Key))==side)total=checked(total+weight.Value);
            return new RegionSideScore(null,0,0,0,total,total);
        }
    }
}
