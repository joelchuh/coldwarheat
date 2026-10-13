using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class SetupDeploymentBoundaryTests
    {
        [Test] public async Task Host_rejects_old_operations_and_events_with_explicit_setup_error()
        {
            var host=new LocalGameHost(SetupDeploymentTests.Started(),true);var session=host.UssrSession;var v=session.ReadView();
            Assert.That(session.QueryInfluenceTargets(v.ContextId,v.Version).Error,Is.EqualTo(RuleError.SetupInProgress));
            Assert.That(session.QueryEventTargets("no-event",v.Version).Error,Is.EqualTo(RuleError.SetupInProgress));
            var op=await session.SubmitAsync(new CommandEnvelope("old-ops",v.GameId,v.Version,v.ContextId,new[]{new CountryId("country.poland")}));
            var ev=await session.SubmitEventAsync(new EventCommandEnvelope("old-event",v.GameId,v.Version,EventCommandKind.Begin,"no-event"));
            Assert.That(op.Error,Is.EqualTo(RuleError.SetupInProgress));Assert.That(ev.Error,Is.EqualTo(RuleError.SetupInProgress));
            Assert.That(session.ReadView().Version,Is.EqualTo(v.Version));
            Assert.That(host.UsSession.ReadView().OwnHand,Is.EqualTo(new[]{"fixture.us.secret"}));
            Assert.That(host.UsSession.ReadView().OpponentHandCount,Is.EqualTo(1));
        }
        [Test] public void Flow_handles_absent_ended_stale_and_mixed_state_without_mutation()
        {
            var d=SetupDeploymentTests.Definition();var empty=SetupDeploymentTests.Empty(d.Map);
            Assert.That(InitialDeploymentFlow.QueryDecision(null,PlayerSide.US).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(InitialDeploymentFlow.QueryDecision(empty,PlayerSide.US).Error,Is.EqualTo(RuleError.SetupNotActive));
            Assert.That(InitialDeploymentFlow.Begin(empty,null,"test").Error,Is.EqualTo(RuleError.InvalidDefinition));
            foreach(var id in new[]{null,"",new string('x',129)})Assert.That(InitialDeploymentFlow.Begin(empty,d,id).Error,Is.EqualTo(RuleError.InvalidCommand));
            var s=InitialDeploymentFlow.Begin(empty,d,"test").NextState;
            var stale=new GameState(s.Map,s.Countries,s.Operations,version:s.Version+1,deployment:s.Deployment);
            Assert.That(InitialDeploymentFlow.QueryDecision(stale,PlayerSide.USSR).Error,Is.EqualTo(RuleError.StaleDecision));
            var ended=new GameState(s.Map,s.Countries,s.Operations,version:s.Version,ended:true,deployment:s.Deployment);
            Assert.That(InitialDeploymentFlow.QueryDecision(ended,PlayerSide.USSR).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
            var effect=new ClearThenControlEffect("card.fidel","effect.fidel",PlayerSide.USSR,new CountryId("country.cuba"));
            var mixed=new GameState(s.Map,s.Countries,s.Operations,version:s.Version,deployment:s.Deployment,influenceEvent:new InfluenceEventExecution("event",effect,PlayerSide.USSR));
            Assert.That(InitialDeploymentFlow.QueryDecision(mixed,PlayerSide.USSR).Error,Is.EqualTo(RuleError.InvalidSetupState));
            Assert.That(InfluenceEventFlow.Begin(mixed,PlayerSide.USSR).Error,Is.EqualTo(RuleError.SetupInProgress));
        }
        [TestCase("null")] [TestCase("empty")] [TestCase("many")] [TestCase("bad-side")]
        [TestCase("zero-fixed")] [TestCase("negative-free")] [TestCase("empty-region")]
        public void Stage_definition_rejects_bad_inputs(string kind)
        {
            IEnumerable<InfluenceAllocation> fixedRows=new[]{new InfluenceAllocation(new CountryId("country.poland"),1)};
            var side=PlayerSide.USSR;var free=6;var region="region.eastern_europe";
            switch(kind)
            {
                case "null":fixedRows=null;break;case "empty":fixedRows=Array.Empty<InfluenceAllocation>();break;
                case "many":fixedRows=Enumerable.Range(0,129).Select(i=>new InfluenceAllocation(new CountryId("country."+i),1));break;
                case "bad-side":side=(PlayerSide)9;break;case "zero-fixed":fixedRows=new[]{new InfluenceAllocation(new CountryId("country.poland"),0)};break;
                case "negative-free":free=-1;break;case "empty-region":region="";break;
            }
            Assert.That(()=>new SetupDeploymentStage(side,fixedRows,region,free),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Definition_checks_map_refs_and_loader_contract_identity()
        {
            var d=SetupDeploymentTests.Definition();
            var bad=new SetupDeploymentStage(PlayerSide.USSR,new[]{new InfluenceAllocation(new CountryId("country.missing"),1)},"region.eastern_europe",6);
            Assert.That(()=>new SetupDeploymentDefinition(d.Map,new[]{bad,d.Stages[1]}),Throws.ArgumentException);
            Assert.That(()=>new SetupDeploymentDefinition(d.Map,d.Stages.Take(1)),Throws.ArgumentException);
            Assert.That(()=>SetupDeploymentLoader.LoadSubset(SetupDeploymentTests.Source(),"{}",d.Map),Throws.ArgumentException);
            Assert.That(()=>SetupDeploymentLoader.LoadSubset("{}",SetupDeploymentTests.Schema(),d.Map),Throws.ArgumentException);
        }
    }
}
