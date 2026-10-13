using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Infrastructure
{
    public sealed class DemoScenario
    {
        public string Id { get; }
        public string Title { get; }
        public PlayerSide Actor { get; }
        public CountryId SelectedCountry { get; }
        public int Ops { get; }
        public InfluenceEffectDefinition EventDefinition { get; }
        public GameTurnContext Turn { get; }
        public SetupDeploymentDefinition DeploymentDefinition { get; }
        public InitialDealResult InitialCards { get; }
        public IReadOnlyDictionary<CountryId,CountryState> Countries { get; }
        public IReadOnlyDictionary<PlayerSide,IReadOnlyList<string>> Hands { get; }
        internal DemoScenario(string id,string title,PlayerSide actor,CountryId selected,int ops,IReadOnlyDictionary<CountryId,CountryState> countries,IReadOnlyDictionary<PlayerSide,IReadOnlyList<string>> hands,InfluenceEffectDefinition eventDefinition=null,GameTurnContext turn=null,SetupDeploymentDefinition deployment=null,InitialDealResult initialCards=null)
        { Id=id;Title=title;Actor=actor;SelectedCountry=selected;Ops=ops;Countries=countries;Hands=hands;EventDefinition=eventDefinition;Turn=turn;DeploymentDefinition=deployment;InitialCards=initialCards; }
    }
    public sealed partial class DemoCatalog
    {
        public CardCatalog PrintedCards { get; }
        public MapDefinition Map { get; }
        public IReadOnlyList<DemoScenario> Scenarios { get; }
        public IReadOnlyDictionary<string,string> Names { get; }
        readonly InfluenceCosts costs;
        DemoCatalog(MapDefinition map,InfluenceCosts costs,IEnumerable<DemoScenario> scenarios,Dictionary<string,string> names,CardCatalog printedCards=null)
        { Map=map;this.costs=costs;Scenarios=System.Array.AsReadOnly(scenarios.ToArray());Names=new ReadOnlyDictionary<string,string>(names);PrintedCards=printedCards; }
        public static DemoCatalog Load(string map,string schema,string scenarios,string costs,string names,string approvedMapProfile)
        {
            var world=MapDefinitionLoader.Load(map,schema,approvedMapProfile);
            var policy=Object(ContractJson.Parse(costs),"schema_version dataset_id content_version description normal_cost enemy_controlled_cost");
            if(Int(policy,"schema_version")!=1 || Text(policy,"dataset_id")!="rules.deluxe2015.influence_placement" || Text(policy,"content_version")!="0.1.0") throw new ArgumentException("Unsupported costs.");
            var rates=new InfluenceCosts(Int(policy,"normal_cost"),Int(policy,"enemy_controlled_cost"));
            var labels=Object(ContractJson.Parse(names),"schema_version dataset_id content_version locale description entries");
            if(Int(labels,"schema_version")!=1 || labels["entries"] is not JObject entries)throw new ArgumentException("Invalid names.");
            var dictionary=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var c in world.Countries) dictionary.Add(c.Id.Value,Text(entries,c.Id.Value+".name"));
            var data=Object(ContractJson.Parse(scenarios),"schema_version dataset_id description scope scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"scope")!="fixture" || Text(data,"dataset_id")!="demo.influence_scenarios")throw new ArgumentException("Demo must be an explicit fixture.");
            var items=new List<DemoScenario>();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var raw in Array(data,"scenarios"))
            {
                items.Add(ReadScenario(raw,world,rates,ids));
            }
            if(items.Count==0 || items.Count>20)throw new ArgumentException("Invalid demo scenario count.");
            return new DemoCatalog(world,rates,items,dictionary);
        }
        public LocalGameHost CreateHost(string scenarioId)
        {
            var s=Scenarios.FirstOrDefault(x=>x.Id==scenarioId)??throw new ArgumentException("Unknown scenario.");
            var ctx=OperationsContext.FromRoundStart(s.Id+".ops",s.Actor,s.Ops,Map,s.Countries,costs);
            var gameId="demo-"+Guid.NewGuid().ToString("N");
            var execution=s.EventDefinition==null?null:new InfluenceEventExecution(gameId+".event",s.EventDefinition,s.Actor,InfluenceEventPolicy.NoActiveInterference);
            var state=new GameState(Map,s.Countries,ctx,gameId,hands:s.Hands,influenceEvent:execution,turn:s.Turn,
                cards:s.InitialCards?.Cards,randomState:s.InitialCards?.Random);
            if(s.DeploymentDefinition!=null)
            {
                var started=InitialDeploymentFlow.Begin(state,s.DeploymentDefinition,gameId+".deployment");
                if(!started.Accepted)throw new ArgumentException("Invalid deployment fixture: "+started.Error);
                state=started.NextState;
            }
            return new LocalGameHost(state,true);
        }
        public DemoCatalog WithEventScenarios(string fixtures,string parameters,string parameterSchema)
        {
            var definitions=EarlyInfluenceCatalogLoader.Load(parameters,parameterSchema,Map);
            return WithEventDefinitions(fixtures,"demo.event_scenarios",definitions);
        }
        public DemoCatalog WithAllocationScenarios(string fixtures,string parameters,string parameterSchema,string cardReferences)
        {
            var definitions=BudgetedRemovalCatalogLoader.Load(parameters,parameterSchema,Map,CardEffectReferenceIndex.Load(cardReferences));
            return WithEventDefinitions(fixtures,"demo.allocation_scenarios",definitions);
        }
        public DemoCatalog WithEraScenarios(string fixtures,string parameters,string parameterSchema,string cardReferences,string turnFlow)
        {
            var calendar=TurnCalendarLoader.Load(turnFlow);
            var definition=EraRemovalCatalogLoader.Load(parameters,parameterSchema,Map,CardEffectReferenceIndex.Load(cardReferences));
            var definitions=new Dictionary<string,InfluenceEffectDefinition>(StringComparer.Ordinal){{definition.CardId,definition}};
            return WithEventDefinitions(fixtures,"demo.era_scenarios",definitions,calendar);
        }
        DemoCatalog WithEventDefinitions(string fixtures,string dataset,IReadOnlyDictionary<string,InfluenceEffectDefinition> definitions,TurnCalendar calendar=null)
        {
            var data=Object(ContractJson.Parse(fixtures),"schema_version dataset_id description scope scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!=dataset || Text(data,"scope")!="fixture")
                throw new ArgumentException("Event lessons must be explicit fixtures.");
            var rows=Array(data,"scenarios");if(rows.Count==0 || rows.Count>20)throw new ArgumentException("Invalid event fixture count.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var all=Scenarios.ToList();
            foreach(var raw in rows)
            {
                var row=raw as JObject??throw new ArgumentException("Invalid event fixture.");
                if(!definitions.TryGetValue(Text(row,"event_card"),out var definition))throw new ArgumentException("Unknown fixture event.");
                all.Add(ReadScenario(row,Map,costs,ids,definition,calendar));
            }
            return new DemoCatalog(Map,costs,all,Names.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal),PrintedCards);
        }
        static DemoScenario ReadScenario(JToken raw,MapDefinition world,InfluenceCosts rates,HashSet<string> ids,InfluenceEffectDefinition eventDefinition=null,TurnCalendar calendar=null)
        {
                var fields=eventDefinition==null?"id title actor ops selected_country influence hands":"id title actor ops selected_country influence hands event_card";
                var s=Object(raw,fields+(calendar==null?"":" turn_number"));var id=Text(s,"id");var title=Text(s,"title");var actor=Side(Text(s,"actor"));var ops=Int(s,"ops");
                var turn=calendar?.ForTurn(Int(s,"turn_number"));
                var selected=new CountryId(Text(s,"selected_country"));
                if(!ids.Add(id) || (eventDefinition==null?(ops<1 || ops>4):ops!=0) || !world.Contains(selected))throw new ArgumentException("Invalid scenario.");
                var countries=world.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));var overrides=new HashSet<CountryId>();
                foreach(var rawPlacement in Array(s,"influence"))
                {
                    var placement=Object(rawPlacement,"country_id us ussr");var cid=new CountryId(Text(placement,"country_id"));
                    if(!world.Contains(cid) || !overrides.Add(cid))throw new ArgumentException("Invalid scenario country.");
                    countries[cid]=new CountryState(Int(placement,"us"),Int(placement,"ussr"));
                }
                var handsData=Object(s["hands"],"US USSR");var hands=new Dictionary<PlayerSide,IReadOnlyList<string>>();
                foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
                {
                    var list=Array(handsData,side.ToString());
                    if(list.Count>20 || list.Any(x=>x.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)x)))throw new ArgumentException("Invalid demo hands.");
                    hands.Add(side,System.Array.AsReadOnly(list.Values<string>().ToArray()));
                }
                var scenario=new DemoScenario(id,title,actor,selected,ops,new ReadOnlyDictionary<CountryId,CountryState>(countries),new ReadOnlyDictionary<PlayerSide,IReadOnlyList<string>>(hands),eventDefinition,turn);
                // Validate the whole fixture before publishing the catalog.
                _=new GameState(world,scenario.Countries,OperationsContext.FromRoundStart(id,actor,ops,world,scenario.Countries,rates),hands:scenario.Hands,turn:turn);
                return scenario;
        }
        static JObject Object(JToken token,string fields)
        {
            var obj=token as JObject??throw new ArgumentException("Expected object.");var expected=fields.Split(' ');
            if(obj.Properties().Count()!=expected.Length || expected.Any(k=>obj.Property(k)==null))throw new ArgumentException("Unexpected object fields.");
            return obj;
        }
        static string Text(JObject obj,string key)=>obj[key]?.Type==JTokenType.String && !string.IsNullOrWhiteSpace((string)obj[key])?(string)obj[key]:throw new ArgumentException("Invalid string: "+key);
        static int Int(JObject obj,string key)
        {
            if(obj[key]?.Type!=JTokenType.Integer)throw new ArgumentException("Invalid integer: "+key);
            try{return (int)obj[key];}catch(OverflowException e){throw new ArgumentException("Integer out of range.",e);}
        }
        static JArray Array(JObject obj,string key)=>obj[key] as JArray??throw new ArgumentException("Invalid array: "+key);
        static PlayerSide Side(string text)=>text=="US"?PlayerSide.US:text=="USSR"?PlayerSide.USSR:throw new ArgumentException("Unknown side.");
    }
}
