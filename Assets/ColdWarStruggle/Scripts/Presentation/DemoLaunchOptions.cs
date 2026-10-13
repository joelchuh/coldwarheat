using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Presentation
{
    public static class DemoLaunchOptions
    {
        public static bool ShowHands(IEnumerable<string> args)
        {
            if(args==null)throw new ArgumentNullException(nameof(args));
            const string prefix="--demo-hands=";
            var choices=args.Where(a=>a!=null && a.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            if(choices.Length==0)return false;
            if(choices.Length!=1)throw new ArgumentException("Only one hand display option may be selected.");
            return choices[0].Substring(prefix.Length) switch {"open"=>true,"closed"=>false,_=>throw new ArgumentException("Unknown hand display option.")};
        }
        public static bool FocusSelected(IEnumerable<string> args,DiagnosticMapKind kind)
        {
            if(args==null)throw new ArgumentNullException(nameof(args));
            if(!Enum.IsDefined(typeof(DiagnosticMapKind),kind))throw new ArgumentOutOfRangeException(nameof(kind));
            const string prefix="--demo-focus=";
            var choices=args.Where(a=>a!=null && a.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            if(choices.Length==0)return false;
            if(choices.Length!=1 || kind!=DiagnosticMapKind.World)throw new ArgumentException("Focus requires a single world-map display option.");
            switch(choices[0].Substring(prefix.Length))
            {
                case "overview":return false;
                case "selected":return true;
                default:throw new ArgumentException("Unknown map focus option.");
            }
        }
        public static DiagnosticMapKind MapKind(IEnumerable<string> args)
        {
            if(args==null)throw new ArgumentException("Missing launch arguments.");
            const string prefix="--demo-map=";
            var values=args.Where(a=>a!=null && a.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            if(values.Length==0)return DiagnosticMapKind.World;
            if(values.Length!=1)throw new ArgumentException("Only one map view may be selected.");
            switch(values[0].Substring(prefix.Length))
            {
                case "tiles":return DiagnosticMapKind.Tiles;
                case "table":return DiagnosticMapKind.Table;
                case "world":return DiagnosticMapKind.World;
                default:throw new ArgumentException("Unknown diagnostic map view.");
            }
        }
        public static int ScenarioIndex(IEnumerable<string> args,IReadOnlyList<string> knownIds)
        {
            if(args==null || knownIds==null || knownIds.Count==0)throw new ArgumentException("Missing demo launch context.");
            const string prefix="--demo-scenario=";
            var choices=args.Where(a=>a!=null && a.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            if(choices.Length==0)
            {
                for(int i=0;i<knownIds.Count;i++)if(knownIds[i]=="demo.setup.initial_deal")return i;
                return 0;
            }
            if(choices.Length!=1)throw new ArgumentException("Only one demo scenario may be selected.");
            var id=choices[0].Substring(prefix.Length);
            for(int i=0;i<knownIds.Count;i++)if(knownIds[i]==id)return i;
            throw new ArgumentException("Unknown demo scenario.");
        }
    }
}
