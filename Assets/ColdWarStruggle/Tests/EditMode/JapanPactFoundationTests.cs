using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class JapanPactFoundationTests
    {
        static CountryId C(string name)=>new CountryId("country."+name);
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/treaty-protection.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/treaty-protection.schema.json");
        internal static JapanPactDefinition Load(MapDefinition map=null,string json=null,CardCatalog cards=null)
            =>JapanPactDefinitionLoader.Load(json??Data(),Schema(),map??DefinitionTests.World(),cards??CardCatalogLoaderTests.Load());
        static AddToControlInfluenceEffect Grant(PlayerSide side=PlayerSide.US)=>new AddToControlInfluenceEffect("card.us_japan_mutual_defense_pact","effect.us_japan_mutual_defense_pact",side,C("japan"));
        static GameState Board(MapDefinition map=null,int us=1,int su=3,long version=0,long sequence=0,bool ended=false,string game="japan-test",GameTurnContext turn=null)
        {
            map??=DefinitionTests.World();var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));countries[C("japan")]=new CountryState(us,su);
            return new GameState(map,countries,new OperationsContext("pact-test",PlayerSide.US,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),game,version,eventSequence:sequence,ended:ended,turn:turn);
        }
        [TestCase(PlayerSide.US,1,3,7,3)][TestCase(PlayerSide.US,10,3,10,3)][TestCase(PlayerSide.US,7,3,7,3)]
        [TestCase(PlayerSide.USSR,3,1,3,7)]
        public void Add_to_control_preserves_opponent_and_excess_and_uses_stability(PlayerSide side,int us,int su,int expectedUs,int expectedSu)
        {
            var s=Board(us:us,su:su,version:12,sequence:25);var p=InfluenceEventRules.Plan(s,Grant(side),null,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.GetInfluence(C("japan"),PlayerSide.US),Is.EqualTo(expectedUs));Assert.That(p.NextState.GetInfluence(C("japan"),PlayerSide.USSR),Is.EqualTo(expectedSu));
            var changed=us!=expectedUs||su!=expectedSu;Assert.That(p.Status,Is.EqualTo(changed?InfluenceEventStatus.ResolvedChange:InfluenceEventStatus.ResolvedNoChange));Assert.That(p.Changes.Count,Is.EqualTo(changed?1:0));Assert.That(p.ResolutionSequence,Is.EqualTo(changed?27:26));Assert.That(p.NextState.Version,Is.EqualTo(13));Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));Assert.That(s.Countries[C("japan")].US,Is.EqualTo(us));
        }
        [TestCase("amount")][TestCase("version")][TestCase("sequence")]
        public void Grant_overflow_never_publishes_partial_state(string fault)
        {
            var s=Board(su:fault=="amount"?int.MaxValue:3,version:fault=="version"?long.MaxValue:0,sequence:fault=="sequence"?long.MaxValue-1:0);
            var p=InfluenceEventRules.Plan(s,Grant(),null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);
        }
        [Test] public void Reusable_grant_reads_a_different_countrys_stability_and_setup_gate()
        {
            var s=Board();var effect=new AddToControlInfluenceEffect("fixture.card","fixture.effect",PlayerSide.US,C("iran"));
            var p=InfluenceEventRules.Plan(s,effect,null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.NextState.GetInfluence(C("iran"),PlayerSide.US),Is.EqualTo(s.Map.GetCountry(C("iran")).Stability));Assert.That(p.NextState.GetInfluence(C("japan"),PlayerSide.US),Is.EqualTo(1));
            var setup=SetupDeploymentTests.Started();var d=Load(setup.Map);var treaty=JapanPactState.Create(d,setup.GameId);
            Assert.That(InfluenceEventRules.Plan(setup,Grant(),null,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.SetupInProgress));
            Assert.That(treaty.Activate(setup,PlayerSide.US,"setup").Error,Is.EqualTo(RuleError.SetupInProgress));Assert.That(JapanPactRules.Evaluate(setup,treaty,TreatyAction.Coup,PlayerSide.USSR,C("japan")).Error,Is.EqualTo(RuleError.SetupInProgress));
        }
        [Test] public void Grant_retains_unknown_policy_selection_ended_and_country_gates()
        {
            var s=Board();Assert.That(InfluenceEventRules.Plan(s,Grant(),new InfluenceEventSelection(PlayerSide.US,0,new[]{C("japan")}),InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
            Assert.That(InfluenceEventRules.Query(s,Grant(),(InfluenceEventPolicy)999).Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));
            Assert.That(InfluenceEventRules.Query(Board(ended:true),Grant(),InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));
            Assert.That(InfluenceEventRules.Query(s,new AddToControlInfluenceEffect("c","e",PlayerSide.US,C("unknown")),InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.InvalidDefinition));
            Assert.That(()=>new AddToControlInfluenceEffect("c","e",PlayerSide.US,default),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Grant_flow_uses_root_actor_but_fixed_US_beneficiary(PlayerSide rootActor)
        {
            var s=Board();var ready=new GameState(s.Map,s.Countries,s.Operations,influenceEvent:new InfluenceEventExecution("pact",Grant(),rootActor,InfluenceEventPolicy.NoActiveInterference));
            var wrong=rootActor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;Assert.That(InfluenceEventFlow.Begin(ready,wrong).Error,Is.EqualTo(RuleError.WrongActor));
            var r=InfluenceEventFlow.Begin(ready,rootActor);Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.Countries[C("japan")].US,Is.EqualTo(7));Assert.That(r.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
        }
        [Test] public void Loader_binds_only_Japan_and_records_immutable_definition_identity()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();var d=Load(map,cards:cards);Assert.That(d.Map,Is.SameAs(map));Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Card.Value,Is.EqualTo("card.us_japan_mutual_defense_pact"));Assert.That(d.EffectId,Is.EqualTo("effect.us_japan_mutual_defense_pact"));Assert.That(d.ImmediateInfluence.Beneficiary,Is.EqualTo(PlayerSide.US));Assert.That(d.ImmediateInfluence.Country,Is.EqualTo(C("japan")));Assert.That(d.Duration,Is.EqualTo("UntilCancelledOrGameEnd"));Assert.That(d.ContentVersion,Is.EqualTo("0.1.0"));Assert.That(d.RulesetId,Is.EqualTo("ruleset.deluxe2015"));Assert.That(d.DataDigest,Does.Match("^[0-9a-f]{64}$"));
            var reformatted=JObject.Parse(Data());Assert.That(Load(map,reformatted.ToString()).DataDigest,Is.EqualTo(d.DataDigest));reformatted["description"]="changed metadata";Assert.That(Load(map,reformatted.ToString()).DataDigest,Is.Not.EqualTo(d.DataDigest));
        }
        [TestCase("profile")][TestCase("scope")][TestCase("ruleset")][TestCase("card")][TestCase("effect")][TestCase("duration")][TestCase("expiry")][TestCase("gate")][TestCase("country")][TestCase("side")][TestCase("target")][TestCase("missing-grant")][TestCase("extra")][TestCase("actor")][TestCase("attack")][TestCase("source-card")][TestCase("missing-prohibition")][TestCase("duplicate-prohibition")][TestCase("exception")][TestCase("pending")][TestCase("unknown-ref")][TestCase("duplicate-ref")][TestCase("missing-ref")][TestCase("source-url")][TestCase("source-duplicate")]
        public void Loader_rejects_schema_valid_unsupported_semantics_and_bad_sources(string fault)
        {
            var j=JObject.Parse(Data());var p=j["parameters"]["us_japan_mutual_defense_pact"];
            switch(fault)
            {
                case "profile":j["content_version"]="0.2.0";break;case "scope":j["scope"]="fixture";break;case "ruleset":j["ruleset_id"]="ruleset.other";break;
                case "card":p["card_id"]="card.nato";break;case "effect":p["effect_id"]="effect.nato";break;case "duration":p["duration"]="UntilTurnEnd";break;case "expiry":p["expiry_boundary"]="ExpireAndReset";break;
                case "gate":p["activation_gate"]["kind"]="AnySuccessfulEvent";((JArray)p["activation_gate"]["card_ids"]).Add("card.nato");break;
                case "country":p["immediate_influence"]["country_id"]="country.france";break;case "side":p["immediate_influence"]["side"]="USSR";break;case "target":p["target_scope"]["country_id"]="country.france";break;case "missing-grant":p["immediate_influence"]=null;break;case "extra":p["script"]="unsupported";break;
                case "actor":p["prohibitions"][0]["actor"]="US";break;case "attack":p["prohibitions"][0]["action"]="EventAttack";break;case "source-card":p["prohibitions"][0]["source_card_id"]="card.brush_war";break;case "missing-prohibition":((JArray)p["prohibitions"]).RemoveAt(1);break;case "duplicate-prohibition":((JArray)p["prohibitions"]).Add(p["prohibitions"][0].DeepClone());break;
                case "exception":((JArray)p["country_exceptions"]).Add(new JObject{{"country_id","country.japan"},{"while_effect_active","effect.nato"}});break;
                case "pending":p["source_refs"][0]["verification"]="Pending";break;case "unknown-ref":p["source_refs"][0]["source_id"]="Unknown";break;case "duplicate-ref":((JArray)p["source_refs"]).Add(p["source_refs"][0].DeepClone());break;case "missing-ref":((JArray)p["source_refs"]).RemoveAt(0);break;case "source-url":j["source_catalog"][0]["url"]="https://example.invalid";break;case "source-duplicate":((JArray)j["source_catalog"]).Add(j["source_catalog"][0].DeepClone());break;
            }
            Assert.That(()=>Load(json:j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Missing_card_map_or_contract_and_invalid_definition_cannot_load()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();Assert.That(()=>JapanPactDefinitionLoader.Load(Data(),Schema(),null,cards),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>JapanPactDefinitionLoader.Load(Data(),Schema().Replace("urn:coldwarheat:contract:treaty-protection:1","unknown"),map,cards),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>Load(map,cards:new CardCatalog(cards.Cards.Where(c=>c.Id.Value!="card.us_japan_mutual_defense_pact"))),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new JapanPactDefinition(map,cards,Grant(PlayerSide.USSR),"ruleset.deluxe2015","0.1.0",new string('a',64)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new JapanPactDefinition(map,cards,Grant(),"ruleset.deluxe2015","0.1.0","bad"),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Activation_retains_source_receipt_and_retries_without_reapplying(PlayerSide actor)
        {
            var d=Load();var s=Board(d.Map,version:12,sequence:30);var empty=JapanPactState.Create(d,s.GameId);var r=empty.Activate(s,actor,"pact-resolution");
            Assert.That(r.Accepted,Is.True);Assert.That(r.After,Is.Not.SameAs(empty));Assert.That(empty.Instance,Is.Null);Assert.That(r.Instance.Definition,Is.SameAs(d));Assert.That(r.Instance.Controller,Is.EqualTo(actor));Assert.That(r.Instance.SourceVersion,Is.EqualTo(12));Assert.That(r.Instance.SourceEventSequence,Is.EqualTo(30));Assert.That(r.Instance.ActivationSequence,Is.EqualTo(31));Assert.That(s.Countries[C("japan")].US,Is.EqualTo(1));
            var retry=r.After.Activate(s,actor,"pact-resolution");Assert.That(retry.AlreadyApplied,Is.True);Assert.That(retry.After,Is.SameAs(r.After));
            Assert.That(r.After.Activate(s,actor,"another").Error,Is.EqualTo(RuleError.EventRequiresRuling));
            Assert.That(r.After.Activate(s,actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,"pact-resolution").Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(r.After.Activate(Board(d.Map,version:13,sequence:30),actor,"pact-resolution").Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(r.After.Activate(Board(d.Map,version:12,sequence:31),actor,"pact-resolution").Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [TestCase("actor",RuleError.InvalidCommand)][TestCase("blank",RuleError.InvalidCommand)][TestCase("space",RuleError.InvalidCommand)][TestCase("long",RuleError.InvalidCommand)][TestCase("game",RuleError.WrongGame)][TestCase("map",RuleError.InvalidDefinition)][TestCase("ended",RuleError.GameAlreadyEnded)][TestCase("overflow",RuleError.NumericOverflow)]
        public void Invalid_activation_has_no_candidate(string fault,RuleError error)
        {
            var d=Load();var t=JapanPactState.Create(d,"japan-test");var s=Board(fault=="map"?DefinitionTests.World():d.Map,game:fault=="game"?"another":"japan-test",ended:fault=="ended",sequence:fault=="overflow"?long.MaxValue:0);
            var id=fault=="blank"?"":fault=="space"?" padded ":fault=="long"?new string('x',129):"valid";var r=t.Activate(s,fault=="actor"?(PlayerSide)99:PlayerSide.US,id);Assert.That(r.Error,Is.EqualTo(error));Assert.That(r.After,Is.Null);Assert.That(t.Instance,Is.Null);
        }
        [TestCase(TreatyAction.Coup,PlayerSide.USSR,"japan",true)][TestCase(TreatyAction.Realignment,PlayerSide.USSR,"japan",true)]
        [TestCase(TreatyAction.Influence,PlayerSide.USSR,"japan",false)][TestCase(TreatyAction.EventAttack,PlayerSide.USSR,"japan",false)]
        [TestCase(TreatyAction.Coup,PlayerSide.US,"japan",false)][TestCase(TreatyAction.Coup,PlayerSide.USSR,"france",false)]
        public void Pact_blocks_only_named_actor_actions_and_country_even_after_control_changes(TreatyAction action,PlayerSide actor,string country,bool blocked)
        {
            var d=Load();var s=Board(d.Map);var empty=JapanPactState.Create(d,s.GameId);Assert.That(JapanPactRules.Evaluate(s,empty,action,actor,C(country)).Status,Is.EqualTo(TreatyRestrictionStatus.NoTreatyBlock));
            var active=empty.Activate(s,PlayerSide.US,"source").After;var later=Board(d.Map,us:0,su:10,turn:PlayScoringTests.Ready().Practice.Definition.Calendar.ForTurn(3));
            var r=JapanPactRules.Evaluate(later,active,action,actor,C(country));Assert.That(r.Status,Is.EqualTo(blocked?TreatyRestrictionStatus.BlockedByTreaty:TreatyRestrictionStatus.NoTreatyBlock));Assert.That(r.EffectId,Is.EqualTo(blocked?d.EffectId:null));Assert.That(later.EventSequence,Is.Zero);
        }
        [TestCase("actor")][TestCase("action")][TestCase("country")][TestCase("map")][TestCase("game")][TestCase("ended")][TestCase("null-board")][TestCase("null-treaty")]
        public void Query_never_turns_invalid_context_into_permission(string fault)
        {
            var d=Load();var s=Board(fault=="map"?DefinitionTests.World():d.Map,game:fault=="game"?"other":"japan-test",ended:fault=="ended");var t=JapanPactState.Create(d,"japan-test");
            var q=JapanPactRules.Evaluate(fault=="null-board"?null:s,fault=="null-treaty"?null:t,fault=="action"?(TreatyAction)99:TreatyAction.Coup,fault=="actor"?(PlayerSide)99:PlayerSide.USSR,C(fault=="country"?"unknown":"japan"));Assert.That(q.Status,Is.EqualTo(TreatyRestrictionStatus.InvalidContext));Assert.That(q.Error,Is.Not.EqualTo(RuleError.None));
        }
    }
}
