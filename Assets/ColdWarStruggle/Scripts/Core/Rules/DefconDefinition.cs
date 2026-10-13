using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum DefconActionKind { Coup, Realignment, Influence, WarEvent }
    public sealed class DefconDefinition
    {
        public MapDefinition Map { get; }
        public int InitialLevel { get; }
        public int MaximumLevel { get; }
        public int NuclearLevel { get; }
        public int TurnStartImprovement { get; }
        public int BattlegroundCoupDegradation { get; }
        public IReadOnlyList<DefconActionKind> RestrictedActions { get; }
        public IReadOnlyDictionary<int,IReadOnlyList<string>> BlockedRegions { get; }
        public DefconDefinition(MapDefinition map,int initial,int maximum,int nuclear,int improvement,int degradation,
            IEnumerable<DefconActionKind> actions,IReadOnlyDictionary<int,IReadOnlyList<string>> blocked)
        {
            if(map==null || actions==null || blocked==null)throw new ArgumentNullException();
            long span=(long)maximum-nuclear;
            // Engineering bound, independent of the configured board track.
            if(nuclear<1 || span<1 || span>64 || initial<=nuclear || initial>maximum
                || improvement<1 || improvement>span || degradation<1 || degradation>span)
                throw new ArgumentOutOfRangeException("Invalid DEFCON track parameters.");
            var copiedActions=actions.Take(3).ToArray();
            if(copiedActions.Length!=2 || copiedActions.Distinct().Count()!=2
                || copiedActions.Any(a=>a!=DefconActionKind.Coup && a!=DefconActionKind.Realignment))
                throw new ArgumentException("Supported restrictions are Coup and Realignment.");
            if(blocked.Count!=span)throw new ArgumentException("Incomplete DEFCON region table.");
            var known=new HashSet<string>(map.Countries.SelectMany(c=>c.Subregions.Concat(new[]{c.Region})),StringComparer.Ordinal);
            var copy=new Dictionary<int,IReadOnlyList<string>>();
            var higher=new HashSet<string>(StringComparer.Ordinal);
            for(int level=maximum;level>nuclear;level--)
            {
                if(!blocked.TryGetValue(level,out var regions) || regions==null)throw new ArgumentException("Missing DEFCON level.");
                var list=regions.Take(known.Count+1).ToArray();
                if(list.Length>known.Count || list.Any(r=>string.IsNullOrWhiteSpace(r) || !known.Contains(r))
                    || list.Distinct(StringComparer.Ordinal).Count()!=list.Length)
                    throw new ArgumentException("Invalid or duplicate DEFCON region.");
                var set=new HashSet<string>(list,StringComparer.Ordinal);
                if((level==maximum && set.Count!=0) || !set.IsSupersetOf(higher))
                    throw new ArgumentException("Restrictions must grow as DEFCON deteriorates.");
                copy.Add(level,Array.AsReadOnly(list.OrderBy(r=>r,StringComparer.Ordinal).ToArray()));
                higher=set;
            }
            Map=map;InitialLevel=initial;MaximumLevel=maximum;NuclearLevel=nuclear;
            TurnStartImprovement=improvement;BattlegroundCoupDegradation=degradation;
            RestrictedActions=Array.AsReadOnly(copiedActions.OrderBy(a=>a).ToArray());
            BlockedRegions=new ReadOnlyDictionary<int,IReadOnlyList<string>>(copy);
        }
    }
    public sealed class DefconState
    {
        public DefconDefinition Definition { get; }
        public int Level { get; }
        public DefconState(DefconDefinition definition,int level)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            if(level<definition.NuclearLevel || level>definition.MaximumLevel)throw new ArgumentOutOfRangeException(nameof(level));
            Definition=definition;Level=level;
        }
    }
}
