using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Full public map facts; no hand/deck facts. The host is responsible for freshness and invocation authority.
    public sealed class ScoringSnapshot
    {
        public MapDefinition Map { get; }
        public string Id { get; }
        public long StateVersion { get; }
        public IReadOnlyDictionary<CountryId,CountryState> Countries { get; }
        public IReadOnlyList<string> ActiveScoringEffects { get; }
        public ScoringSnapshot(MapDefinition map,string id,long stateVersion,IReadOnlyDictionary<CountryId,CountryState> countries,
            IEnumerable<string> activeScoringEffects)
        {
            if(map==null || countries==null || activeScoringEffects==null)throw new ArgumentNullException();
            Id=ScoringGuard.Id(id);
            if(stateVersion<0 || map.Countries.Count>512 || countries.Count!=map.Countries.Count || countries.Keys.Any(c=>!map.Contains(c)))
                throw new ArgumentException("Snapshot must cover exactly one complete map and nonnegative version.");
            var effects=activeScoringEffects.Take(129).ToArray();
            if(effects.Length>128 || effects.Any(e=>string.IsNullOrWhiteSpace(e) || e.Length>256) || effects.Distinct(StringComparer.Ordinal).Count()!=effects.Length)
                throw new ArgumentException("Invalid scoring effect list.");
            Map=map;StateVersion=stateVersion;Countries=new ReadOnlyDictionary<CountryId,CountryState>(countries.ToDictionary(c=>c.Key,c=>c.Value));
            ActiveScoringEffects=Array.AsReadOnly(effects);
        }
    }
}
