using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Tests
{
    public static class InfluenceFixture
    {
        public static CountryId X => new CountryId("X");
        public static CountryId Y => new CountryId("Y");
        public static CountryId Z => new CountryId("Z");
        public static GameState Create(int budget=4,bool chain=false,OperationsMode mode=OperationsMode.Influence,bool ended=false,int usAtX=0)
        {
            var map=new MapDefinition(new[]{new CountryDefinition(X,2,"r",false),new CountryDefinition(Y,2,"r",false),new CountryDefinition(Z,2,"r",false)},
                new[]{new KeyValuePair<CountryId,CountryId>(X,Y),new KeyValuePair<CountryId,CountryId>(Y,Z)},
                new Dictionary<PlayerSide,IReadOnlyList<CountryId>> { [PlayerSide.US]=chain?new CountryId[0]:new[]{X}, [PlayerSide.USSR]=new[]{Z} });
            var countries=new Dictionary<CountryId,CountryState>{[X]=new CountryState(chain?1:usAtX,chain?0:2),[Y]=new CountryState(0,0),[Z]=new CountryState(0,0)};
            var ctx=OperationsContext.FromRoundStart("ops",PlayerSide.US,budget,map,countries,new InfluenceCosts(1,2));
            if(mode!=OperationsMode.Influence)ctx=new OperationsContext("ops",PlayerSide.US,budget,ctx.Reachable,mode,ctx.Costs);
            return new GameState(map,countries,ctx,ended:ended,hands:new Dictionary<PlayerSide,IReadOnlyList<string>>{[PlayerSide.US]=new[]{"test.us.card"},[PlayerSide.USSR]=new[]{"test.ussr.card"}});
        }
    }
}
