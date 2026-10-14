using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public readonly struct CountryState
    {
        public int US { get; }
        public int USSR { get; }
        public CountryState(int us,int ussr) { if(us<0 || ussr<0) throw new ArgumentOutOfRangeException(); US=us; USSR=ussr; }
        public int For(PlayerSide side) => side==PlayerSide.US?US:side==PlayerSide.USSR?USSR:throw new ArgumentOutOfRangeException(nameof(side));
    }
    public sealed class GameState
    {
        public MapDefinition Map { get; }
        public OperationsContext Operations { get; }
        public string GameId { get; }
        public long Version { get; }
        public long EventSequence { get; }
        public int SpentOps { get; }
        public bool Ended { get; }
        public InfluenceEventExecution InfluenceEvent { get; }
        public GameTurnContext Turn { get; }
        public SetupDeploymentExecution Deployment { get; }
        public CardZones Cards { get; }
        public ActionPracticeState Practice { get; }
        public RandomSnapshot RandomState { get; }
        public IReadOnlyDictionary<CountryId,CountryState> Countries { get; }
        readonly Dictionary<PlayerSide,IReadOnlyList<string>> hands=new Dictionary<PlayerSide,IReadOnlyList<string>>();
        public GameState(MapDefinition map, IReadOnlyDictionary<CountryId,CountryState> countries, OperationsContext context,
            string gameId="fixture", long version=0,int spentOps=0,long eventSequence=0,bool ended=false,
            IReadOnlyDictionary<PlayerSide,IReadOnlyList<string>> hands=null,InfluenceEventExecution influenceEvent=null,GameTurnContext turn=null,
            SetupDeploymentExecution deployment=null,CardZones cards=null,RandomSnapshot randomState=null,ActionPracticeState practice=null)
        {
            if(map==null || countries==null || context==null || string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("Missing state data.");
            if(version<0 || eventSequence<0 || spentOps<0 || spentOps>context.Budget) throw new ArgumentOutOfRangeException();
            if(countries.Count!=map.Countries.Count || countries.Keys.Any(x=>!map.Contains(x)) || context.Reachable.Any(x=>!map.Contains(x)))
                throw new ArgumentException("State must cover the map exactly.");
            if((cards==null)!=(randomState==null) || (cards!=null && hands!=null))
                throw new ArgumentException("Cards and random state must be paired, without fixture hands.");
            if(practice?.Definition.InfluenceEvents!=null && !ReferenceEquals(map,practice.Definition.InfluenceEvents.Map))throw new ArgumentException("Practice event map mismatch.");
            if(practice?.Risk!=null && !ReferenceEquals(map,practice.Risk.Definition.Defcon.Map))throw new ArgumentException("Practice risk map mismatch.");
            if(practice?.Definition.Scoring!=null && !ReferenceEquals(map,practice.Definition.Scoring.Map))throw new ArgumentException("Practice scoring map mismatch.");
            Map=map;Operations=context;GameId=gameId;Version=version;SpentOps=spentOps;EventSequence=eventSequence;Ended=ended;
            Cards=cards;RandomState=randomState;Practice=practice;
            if(practice!=null && (cards==null || turn==null || turn.Number!=practice.Space.Turn.Number))throw new ArgumentException("Practice requires real cards and the matching turn.");
            if(deployment!=null && !ReferenceEquals(map,deployment.Definition.Map))throw new ArgumentException("Deployment map mismatch.");
            InfluenceEvent=influenceEvent;Turn=turn;Deployment=deployment;
            Countries=new ReadOnlyDictionary<CountryId,CountryState>(countries.ToDictionary(x=>x.Key,x=>x.Value));
            if(hands!=null && (hands.Count!=2 || !hands.ContainsKey(PlayerSide.US) || !hands.ContainsKey(PlayerSide.USSR))) throw new ArgumentException("Invalid hands.");
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                // String IDs preserve the existing projection API; Cards remains authoritative.
                var list=cards!=null?cards.GetHand(side).Select(id=>id.Value).ToArray():hands==null?new string[0]:hands[side]?.ToArray();
                if(list==null || list.Any(string.IsNullOrWhiteSpace) || list.Distinct(StringComparer.Ordinal).Count()!=list.Length) throw new ArgumentException("Invalid hand instance IDs.");
                this.hands.Add(side,Array.AsReadOnly(list));
            }
        }
        public int GetInfluence(CountryId id,PlayerSide side) => Countries.TryGetValue(id,out var value)?value.For(side):throw new ArgumentException("Unknown country.");
        public IReadOnlyList<string> GetHand(PlayerSide side) => hands.TryGetValue(side,out var h)?h:throw new ArgumentOutOfRangeException(nameof(side));
        internal GameState Apply(IReadOnlyDictionary<CountryId,CountryState> countries,int spent,int count)
            => new GameState(Map,countries,Operations,GameId,checked(Version+1),spent,checked(EventSequence+count),Ended,Cards==null?hands:null,InfluenceEvent,Turn,Deployment,Cards,RandomState,Practice);
        internal GameState WithPractice(ActionPracticeState practice,OperationsContext operations,CardZones cards,RandomSnapshot random,IReadOnlyDictionary<CountryId,CountryState> countries=null,bool? ended=null)
            =>new GameState(Map,countries??Countries,operations,GameId,checked(Version+1),0,checked(EventSequence+1),ended??Ended,
                influenceEvent:InfluenceEvent,turn:Turn,cards:cards,randomState:random,practice:practice);
        internal GameState WithInfluenceEvent(InfluenceEventExecution execution)
            => new GameState(Map,Countries,Operations,GameId,Version,SpentOps,EventSequence,Ended,Cards==null?hands:null,execution,Turn,Deployment,Cards,RandomState,Practice);
        internal GameState WithDeployment(SetupDeploymentExecution deployment)
            => new GameState(Map,Countries,Operations,GameId,Version,SpentOps,EventSequence,Ended,Cards==null?hands:null,InfluenceEvent,Turn,deployment,Cards,RandomState,Practice);
    }
}
