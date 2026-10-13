using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum RegionTier { None, Presence, Domination, Control }
    internal static class ScoringGuard
    {
        internal static string Id(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length>256)throw new ArgumentException("Scoring identifier must be nonempty and at most 256 characters.");
            return value;
        }
    }
    public sealed class RegionScoreTable
    {
        public string Id { get; }
        public int Presence { get; }
        public int Domination { get; }
        public int? Control { get; }
        public RegionScoreTable(string id,int presence,int domination,int? control)
        {
            Id=ScoringGuard.Id(id);
            if(presence<0 || domination<0 || control<0 || (id=="region.europe")!=(!control.HasValue))
                throw new ArgumentException("Invalid region score table or European control outcome.");
            Presence=presence;Domination=domination;Control=control;
        }
    }
    public sealed class RegionScoringDefinition
    {
        public MapDefinition Map { get; }
        public int BattlegroundBonus { get; }
        public int EnemyAdjacencyBonus { get; }
        public IReadOnlyList<RegionScoreTable> Regions { get; }
        public string WeightedRegionId { get; }
        public string WeightedParentId { get; }
        public IReadOnlyDictionary<CountryId,int> CountryWeights { get; }
        public RegionScoringDefinition(MapDefinition map,int battlegroundBonus,int adjacencyBonus,IEnumerable<RegionScoreTable> regions,
            string weightedRegionId,string weightedParentId,IEnumerable<KeyValuePair<CountryId,int>> weights)
        {
            if(map==null || regions==null || weights==null)throw new ArgumentNullException();
            if(map.Countries.Count>512 || battlegroundBonus<0 || adjacencyBonus<0)throw new ArgumentException("Scoring definition limit.");
            WeightedRegionId=ScoringGuard.Id(weightedRegionId);WeightedParentId=ScoringGuard.Id(weightedParentId);
            var tables=regions.Take(513).ToArray();var primary=new HashSet<string>(map.Countries.Select(c=>c.Region),StringComparer.Ordinal);
            if(tables.Length>512 || tables.Any(t=>t==null) || tables.Select(t=>t.Id).Distinct(StringComparer.Ordinal).Count()!=tables.Length
                || !primary.SetEquals(tables.Select(t=>t.Id)) || !primary.Contains(weightedParentId) || primary.Contains(weightedRegionId)
                || tables.Any(t=>!map.GetCountriesInRegion(t.Id).Any(c=>c.Battleground))
                || map.Countries.Any(c=>c.Subregions.Any(s=>primary.Contains(s) && s!=c.Region)))
                throw new ArgumentException("Standard scoring regions must exactly cover primary regions with battlegrounds.");
            var entries=weights.Take(513).ToArray();var members=map.GetCountriesInRegion(weightedRegionId);
            if(entries.Length>512 || members.Count==0 || members.Any(c=>c.Region!=weightedParentId) || entries.Any(w=>w.Value<1)
                || entries.Select(w=>w.Key).Distinct().Count()!=entries.Length || !new HashSet<CountryId>(members.Select(c=>c.Id)).SetEquals(entries.Select(w=>w.Key)))
                throw new ArgumentException("Weights must exactly cover the child region of the declared parent.");
            Map=map;BattlegroundBonus=battlegroundBonus;EnemyAdjacencyBonus=adjacencyBonus;
            Regions=Array.AsReadOnly(tables.OrderBy(t=>t.Id,StringComparer.Ordinal).ToArray());
            CountryWeights=new ReadOnlyDictionary<CountryId,int>(entries.ToDictionary(w=>w.Key,w=>w.Value));
        }
    }
}
