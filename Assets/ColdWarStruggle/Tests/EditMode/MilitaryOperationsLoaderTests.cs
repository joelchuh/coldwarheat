using System;
using System.IO;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class MilitaryOperationsLoaderTests
    {
        public static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/military-operations.json"));
        [Test] public void Actual_data_drives_credit_sources_and_current_defcon_assessment()
        {
            var d=MilitaryOperationsLoader.LoadSubset(Source());Assert.That(d.InitialEarned,Is.Zero);Assert.That(d.ResetEarned,Is.Zero);
            Assert.That(d.PenaltyVpPerShortfall,Is.EqualTo(1));Assert.That(d.CreditSources.Count,Is.EqualTo(7));
            Assert.That(d.CreditSources[MilitaryCreditKind.OperationsCoup],Is.EqualTo(MilitaryCreditBasis.EffectiveMilitaryOperationsCredit));
            Assert.That(d.CreditSources[MilitaryCreditKind.WarEvent],Is.EqualTo(MilitaryCreditBasis.VerifiedCardParameter));
            Assert.That(d.CreditSources[MilitaryCreditKind.FreeCoup],Is.EqualTo(MilitaryCreditBasis.None));
            var state=MilitaryOperationsTests.Earned(2,1,d);
            var assessment=MilitaryOperationsService.AssessTurnEnd(state,"end",new DefconState(DefconTests.Definition(),4));
            Assert.That(assessment.VpDeltaToApply,Is.EqualTo(1));Assert.That(assessment.After.UsEarned,Is.Zero);
            var next=MilitaryOperationsState.Create(d,TurnCalendarTests.Calendar().ForTurn(2));
            Assert.That(next.UsEarned,Is.Zero);Assert.That(next.AppliedCredits.Count,Is.Zero);Assert.That(next.Assessment,Is.Null);
            Assert.That(()=>MilitaryOperationsService.PlanCredit(next,MilitaryOperationsTests.Credit("old",1)),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Loader_consumes_shared_parameters_instead_of_recreating_constants()
        {
            var j=JObject.Parse(Source());j["initial_earned"]=1;j["reset_earned"]=2;j["penalty_vp_per_shortfall"]=2;
            var d=MilitaryOperationsLoader.LoadSubset(j.ToString());
            var s=MilitaryOperationsService.PlanCredit(MilitaryOperationsTests.State(d),MilitaryOperationsTests.Credit("plus",3)).After;
            var p=MilitaryOperationsService.AssessTurnEnd(s,"end",new DefconState(DefconTests.Definition(),5));
            Assert.That(p.Assessment.NetVpForUS,Is.EqualTo(6));Assert.That(p.After.UsEarned,Is.EqualTo(2));
        }
        [TestCase("schema")] [TestCase("id")] [TestCase("status")] [TestCase("scope")] [TestCase("missing-field")] [TestCase("extra-field")]
        [TestCase("source-id")] [TestCase("source-url")] [TestCase("source-date")] [TestCase("source-section")] [TestCase("source-duplicate-section")] [TestCase("source-extra")]
        [TestCase("initial")] [TestCase("reset")] [TestCase("penalty")] [TestCase("string-int")] [TestCase("float-int")] [TestCase("huge-int")]
        [TestCase("boundary")] [TestCase("requirement")] [TestCase("storage")] [TestCase("missing-kind")] [TestCase("extra-kind")]
        [TestCase("unknown-basis")] [TestCase("numeric-basis")] [TestCase("null-sources")] [TestCase("missing-source")]
        public void Unknown_or_malformed_profiles_are_rejected(string mutation)
        {
            var j=JObject.Parse(Source());var source=(JObject)j["source"];var credits=(JObject)j["credit_sources"];
            switch(mutation)
            {
                case "schema":j["schema_version"]=2;break;case "id":j["id"]="other";break;case "status":j["status"]="pending";break;
                case "scope":j["scope"]=false;break;case "missing-field":j.Remove("reset_earned");break;case "extra-field":j["max_earned"]=5;break;
                case "source-id":source["id"]="UNKNOWN";break;case "source-url":source["url"]="https://example.org";break;
                case "source-date":source["checked_on"]="2026-99-99";break;case "source-section":source["sections"]=new JArray("unknown");break;
                case "source-duplicate-section":source["sections"]=new JArray("4.5E","4.5E","7.5","8.2");break;case "source-extra":source["verified"]=true;break;
                case "initial":j["initial_earned"]=-1;break;case "reset":j["reset_earned"]=-1;break;case "penalty":j["penalty_vp_per_shortfall"]=0;break;
                case "string-int":j["penalty_vp_per_shortfall"]="1";break;case "float-int":j["penalty_vp_per_shortfall"]=1.0;break;
                case "huge-int":j["penalty_vp_per_shortfall"]=long.MaxValue;break;case "boundary":j["reset_boundary"]="TurnStart";break;
                case "requirement":j["requirement_source"]="InitialDefcon";break;case "storage":j["storage_policy"]="CapAtFive";break;
                case "missing-kind":credits.Remove("Space");break;case "extra-kind":credits["Unknown"]="None";break;
                case "unknown-basis":credits["WarEvent"]="PrintedOps";break;case "numeric-basis":credits["WarEvent"]=0;break;
                case "null-sources":j["credit_sources"]=null;break;case "missing-source":j.Remove("source");break;
            }
            Assert.That(()=>MilitaryOperationsLoader.LoadSubset(j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Duplicate_keys_and_invalid_roots_are_rejected()
        {
            var duplicate=Source().Replace("\"initial_earned\": 0","\"initial_earned\": 0, \"initial_earned\": 1");
            Assert.That(duplicate,Is.Not.EqualTo(Source()));
            Assert.That(()=>MilitaryOperationsLoader.LoadSubset(duplicate),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsLoader.LoadSubset("[]"),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsLoader.LoadSubset(null),Throws.InstanceOf<ArgumentException>());
        }
    }
}
