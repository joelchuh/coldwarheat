using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class SetupDeploymentStage
    {
        public PlayerSide Side { get; }
        public IReadOnlyList<InfluenceAllocation> FixedInfluence { get; }
        public string Region { get; }
        public int FreeAmount { get; }
        public SetupDeploymentStage(PlayerSide side,IEnumerable<InfluenceAllocation> fixedInfluence,string region,int freeAmount)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side));
            if(freeAmount<1)throw new ArgumentOutOfRangeException(nameof(freeAmount));
            if(string.IsNullOrWhiteSpace(region) || fixedInfluence==null)throw new ArgumentException("Missing deployment definition.");
            var copy=fixedInfluence.Take(129).ToArray();
            if(copy.Length==0 || copy.Length>128 || copy.Any(a=>string.IsNullOrWhiteSpace(a.Country.Value) || a.Amount<1)
                || copy.Select(a=>a.Country).Distinct().Count()!=copy.Length)throw new ArgumentException("Invalid fixed deployment.");
            Side=side;Region=region;FreeAmount=freeAmount;
            FixedInfluence=Array.AsReadOnly(copy.OrderBy(a=>a.Country.Value,StringComparer.Ordinal).ToArray());
        }
    }
    public sealed class SetupDeploymentDefinition
    {
        public MapDefinition Map { get; }
        public IReadOnlyList<SetupDeploymentStage> Stages { get; }
        public SetupDeploymentDefinition(MapDefinition map,IEnumerable<SetupDeploymentStage> stages)
        {
            if(map==null || stages==null)throw new ArgumentNullException();
            var copy=stages.Take(3).ToArray();
            if(copy.Length!=2 || copy.Any(s=>s==null) || copy[0].Side!=PlayerSide.USSR || copy[1].Side!=PlayerSide.US)
                throw new ArgumentException("Deployment requires USSR then US.");
            foreach(var stage in copy)
                if(stage.FixedInfluence.Any(a=>!map.Contains(a.Country)) || map.GetCountriesInRegion(stage.Region).Count==0)
                    throw new ArgumentException("Unknown deployment country or region.");
            Map=map;Stages=Array.AsReadOnly(copy);
        }
    }
}
