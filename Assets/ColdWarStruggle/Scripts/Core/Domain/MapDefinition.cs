using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class CountryDefinition
    {
        public CountryId Id { get; }
        public int Stability { get; }
        public string Region { get; }
        public bool Battleground { get; }
        public IReadOnlyList<string> Subregions { get; }
        public CountryDefinition(CountryId id, int stability, string region, bool battleground, IEnumerable<string> subregions = null)
        {
            if (string.IsNullOrWhiteSpace(id.Value) || string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Definition ID required.");
            if (stability < 1) throw new ArgumentOutOfRangeException(nameof(stability));
            Id = id; Stability = stability; Region = region; Battleground = battleground;
            Subregions = Array.AsReadOnly((subregions ?? Enumerable.Empty<string>()).ToArray());
        }
    }
    public sealed class MapDefinition
    {
        readonly Dictionary<CountryId, CountryDefinition> countries = new Dictionary<CountryId, CountryDefinition>();
        readonly Dictionary<CountryId, IReadOnlyList<CountryId>> neighbors = new Dictionary<CountryId, IReadOnlyList<CountryId>>();
        readonly Dictionary<PlayerSide, IReadOnlyList<CountryId>> powers = new Dictionary<PlayerSide, IReadOnlyList<CountryId>>();
        public IReadOnlyList<CountryDefinition> Countries { get; }
        public MapDefinition(IEnumerable<CountryDefinition> countries, IEnumerable<KeyValuePair<CountryId, CountryId>> edges,
            IReadOnlyDictionary<PlayerSide, IReadOnlyList<CountryId>> superpowers)
        {
            if (countries == null || edges == null || superpowers == null) throw new ArgumentNullException();
            foreach (var c in countries)
            {
                if (c == null || this.countries.ContainsKey(c.Id)) throw new ArgumentException("Duplicate country.");
                this.countries.Add(c.Id,c);
            }
            if (this.countries.Count == 0) throw new ArgumentException("Empty map.");
            var adjacency = this.countries.Keys.ToDictionary(x=>x,x=>new HashSet<CountryId>());
            foreach(var edge in edges)
            {
                if (!Contains(edge.Key) || !Contains(edge.Value) || StringComparer.Ordinal.Compare(edge.Key.Value,edge.Value.Value)>=0
                    || !adjacency[edge.Key].Add(edge.Value)) throw new ArgumentException("Invalid or duplicate edge.");
                adjacency[edge.Value].Add(edge.Key);
            }
            foreach(var pair in adjacency) neighbors.Add(pair.Key,Array.AsReadOnly(pair.Value.OrderBy(x=>x.Value,StringComparer.Ordinal).ToArray()));
            if (superpowers.Count != 2) throw new ArgumentException("Two superpowers required.");
            foreach(PlayerSide side in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                if(!superpowers.TryGetValue(side,out var links) || links==null || links.Any(x=>!Contains(x)) || links.Distinct().Count()!=links.Count)
                    throw new ArgumentException("Invalid superpower links.");
                powers.Add(side,Array.AsReadOnly(links.OrderBy(x=>x.Value,StringComparer.Ordinal).ToArray()));
            }
            Countries=Array.AsReadOnly(this.countries.Values.OrderBy(x=>x.Id.Value,StringComparer.Ordinal).ToArray());
        }
        public bool Contains(CountryId id) => countries.ContainsKey(id);
        public CountryDefinition GetCountry(CountryId id) => countries.TryGetValue(id,out var c) ? c : throw new ArgumentException("Unknown country.");
        public IReadOnlyList<CountryId> GetCountryNeighbors(CountryId id) => neighbors.TryGetValue(id,out var n) ? n : throw new ArgumentException("Unknown country.");
        public IReadOnlyList<CountryId> GetSuperpowerNeighbors(PlayerSide side) => powers.TryGetValue(side,out var n) ? n : throw new ArgumentOutOfRangeException(nameof(side));
        public IReadOnlyList<CountryDefinition> GetCountriesInRegion(string region)
            => Array.AsReadOnly(Countries.Where(c=>c.Region==region || c.Subregions.Contains(region)).ToArray());
    }
}
