using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum OperationsMode { Influence, Coup, Realignment }
    public sealed class OperationsContext
    {
        public string Id { get; }
        public PlayerSide Actor { get; }
        public int Budget { get; }
        public OperationsMode Mode { get; }
        public InfluenceCosts Costs { get; }
        public IReadOnlyList<CountryId> Reachable { get; }
        public OperationsContext(string id,PlayerSide actor,int budget,IEnumerable<CountryId> reachable,OperationsMode mode=OperationsMode.Influence,InfluenceCosts costs=null)
        {
            if(string.IsNullOrWhiteSpace(id) || reachable==null) throw new ArgumentException("Invalid operations context.");
            if((actor!=PlayerSide.US && actor!=PlayerSide.USSR) || budget<0 || !Enum.IsDefined(typeof(OperationsMode),mode)) throw new ArgumentOutOfRangeException();
            Id=id;Actor=actor;Budget=budget;Mode=mode;
            Costs=costs??throw new ArgumentNullException(nameof(costs));
            Reachable=Array.AsReadOnly(reachable.Distinct().OrderBy(x=>x.Value,StringComparer.Ordinal).ToArray());
        }
        public static OperationsContext FromRoundStart(string id,PlayerSide actor,int budget,MapDefinition map,IReadOnlyDictionary<CountryId,CountryState> countries,InfluenceCosts costs)
        {
            if(map==null || countries==null || countries.Count!=map.Countries.Count || countries.Keys.Any(x=>!map.Contains(x))) throw new ArgumentException("Invalid round snapshot.");
            var reachable=new HashSet<CountryId>(map.GetSuperpowerNeighbors(actor));
            foreach(var pair in countries.Where(x=>x.Value.For(actor)>0))
            { reachable.Add(pair.Key);foreach(var neighbor in map.GetCountryNeighbors(pair.Key))reachable.Add(neighbor); }
            return new OperationsContext(id,actor,budget,reachable,costs:costs);
        }
    }
}
