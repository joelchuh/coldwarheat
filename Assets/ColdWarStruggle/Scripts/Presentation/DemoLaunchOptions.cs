using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Presentation
{
    public static class DemoLaunchOptions
    {
        public static int ScenarioIndex(IEnumerable<string> args,IReadOnlyList<string> knownIds)
        {
            if(args==null || knownIds==null || knownIds.Count==0)throw new ArgumentException("Missing demo launch context.");
            const string prefix="--demo-scenario=";
            var choices=args.Where(a=>a!=null && a.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            if(choices.Length==0)return 0;
            if(choices.Length!=1)throw new ArgumentException("Only one demo scenario may be selected.");
            var id=choices[0].Substring(prefix.Length);
            for(int i=0;i<knownIds.Count;i++)if(knownIds[i]==id)return i;
            throw new ArgumentException("Unknown demo scenario.");
        }
    }
}
