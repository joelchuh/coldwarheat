using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class VictoryDefinitionLoaderTests
    {
        public static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/victory.json"));
        [Test] public void Actual_shared_matrix_preserves_case_ids_and_user_CF003_authority()
        {
            var d=VictoryDefinitionLoader.LoadSubset(Source());
            Assert.That(d.InitialSignedVp,Is.Zero);Assert.That(d.AutomaticVpThreshold,Is.EqualTo(20));Assert.That(d.HeldScoringRows.Count,Is.EqualTo(12));
            var expected=new PlayerSide?[]{null,PlayerSide.US,PlayerSide.USSR,PlayerSide.USSR,PlayerSide.USSR,PlayerSide.USSR,
                PlayerSide.US,PlayerSide.US,PlayerSide.US,PlayerSide.US,PlayerSide.USSR,PlayerSide.US};
            for(int i=0;i<12;i++)
            {
                var row=d.HeldScoringRows.Single(x=>x.Id=="RV-H-"+(i+1).ToString("D2"));
                var pending=VictoryTests.Pending(d,row.MilitaryCandidate?.ToString());
                var result=VictoryService.Evaluate(d,new HeldScoringAuditedEvidence("audit."+i,"held",pending,row.UsHolds,row.UssrHolds));
                Assert.That(result.Winner,Is.EqualTo(expected[i]),row.Id);
                Assert.That(result.Outcome,Is.EqualTo(i==0?VictoryOutcome.Continue:VictoryOutcome.Finished));
                Assert.That(result.HeldScoringRowId,Is.EqualTo(row.Id));
            }
            var cf=d.HeldScoringRows.Single(x=>x.Id=="RV-H-11");
            Assert.That(cf.Authority,Is.EqualTo("CF-003"));Assert.That(cf.AdoptionStatus,Is.EqualTo("adopted-project-interpretation"));
        }
        [Test] public void Shared_values_are_consumed_instead_of_hardcoded_winners_or_threshold()
        {
            var j=JObject.Parse(Source());j["initial_signed_vp"]=1;j["automatic_vp_threshold"]=7;
            j["held_scoring_rows"][9]["winner"]="USSR";
            var d=VictoryDefinitionLoader.LoadSubset(j.ToString());Assert.That(d.InitialSignedVp,Is.EqualTo(1));
            Assert.That(VictoryService.Evaluate(d,new ClosedVpBatchEvidence("vp","event",6,1,0)).Winner,Is.EqualTo(PlayerSide.US));
            Assert.That(VictoryService.Evaluate(d,new HeldScoringAuditedEvidence("audit","held",VictoryTests.Pending(d),true,true)).Winner,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase("schema")] [TestCase("id")] [TestCase("status")] [TestCase("scope")] [TestCase("extra-root")] [TestCase("missing-root")]
        [TestCase("positive")] [TestCase("negative")] [TestCase("tie")] [TestCase("initial")] [TestCase("threshold")]
        [TestCase("int-string")] [TestCase("int-float")] [TestCase("int-huge")]
        [TestCase("source-nonobject")] [TestCase("source-null")] [TestCase("source-count")] [TestCase("source-id")] [TestCase("source-url")] [TestCase("source-date")] [TestCase("source-section")]
        [TestCase("source-extra")] [TestCase("faq-page")] [TestCase("policy")] [TestCase("excluded")]
        [TestCase("row-missing")] [TestCase("row-extra")] [TestCase("row-duplicate")] [TestCase("row-id")] [TestCase("row-bool")]
        [TestCase("row-candidate")] [TestCase("row-winner")] [TestCase("row-outcome")] [TestCase("row-authority")] [TestCase("row-adoption")]
        [TestCase("row-field")] [TestCase("row-pending-winner")]
        [TestCase("project-id")] [TestCase("project-status")] [TestCase("project-approver")] [TestCase("project-date")]
        [TestCase("project-case")] [TestCase("project-missing")] [TestCase("project-basis")] [TestCase("project-link")]
        public void Unknown_or_malformed_profile_is_rejected(string mutation)
        {
            var j=JObject.Parse(Source());var source=(JObject)j["sources"][0];var row=(JObject)j["held_scoring_rows"][0];var project=(JObject)j["project_adjudications"][0];
            switch(mutation)
            {
                case "schema":j["schema_version"]=1;break;case "id":j["id"]="other";break;case "status":j["status"]="pending";break;case "scope":j["scope"]=false;break;
                case "extra-root":j["code"]="eval";break;case "missing-root":j.Remove("scope");break;case "positive":j["positive_side"]="USSR";break;
                case "negative":j["negative_side"]=1;break;case "tie":j["final_tie_score"]=1;break;case "initial":j["initial_signed_vp"]=20;break;
                case "threshold":j["automatic_vp_threshold"]=0;break;case "int-string":j["automatic_vp_threshold"]="20";break;case "int-float":j["automatic_vp_threshold"]=20.0;break;
                case "int-huge":j["automatic_vp_threshold"]=long.MaxValue;break;case "source-nonobject":j["sources"][0]=true;break;case "source-null":j["sources"][0]=JValue.CreateNull();break;case "source-count":j["sources"]=new JArray(source.DeepClone());break;
                case "source-id":source["id"]="Unknown";break;case "source-url":source["url"]="https://example.org";break;case "source-date":source["checked_on"]="2026-99-99";break;
                case "source-section":source["sections"]=new JArray("10.3","10.3","10.3","10.3","10.3");break;case "source-extra":source["verified"]=true;break;
                case "faq-page":j["sources"][1]["pdf_pages"]=new JArray(21);break;case "policy":j["product_policy"]="public hand reveal";break;
                case "excluded":j["excluded_answer"]="apply old FAQ";break;case "row-missing":((JArray)j["held_scoring_rows"]).RemoveAt(0);break;
                case "row-extra":((JArray)j["held_scoring_rows"]).Add(row.DeepClone());break;case "row-duplicate":j["held_scoring_rows"][1]=row.DeepClone();break;
                case "row-id":row["case_id"]="unknown";break;case "row-bool":row["us_holds"]="false";break;case "row-candidate":row["military_candidate"]=0;break;
                case "row-winner":row["winner"]="unknown";break;case "row-outcome":row["outcome"]="Draw";break;case "row-authority":row["authority"]="";break;
                case "row-adoption":row["adoption_status"]="unknown";break;case "row-field":row["expr"]="true";break;
                case "row-pending-winner":row["outcome"]="UnresolvedAdjudication";row["winner"]="US";row["adoption_status"]="pending";break;
                case "project-id":project["id"]="unknown";break;case "project-status":project["status"]="pending";break;
                case "project-approver":project["approved_by"]="assistant";break;case "project-date":project["approved_on"]="yesterday";break;
                case "project-case":project["case_id"]="RV-H-10";break;case "project-missing":j["project_adjudications"]=new JArray();break;
                case "project-basis":project["basis"]=new JArray();break;case "project-link":j["held_scoring_rows"][10]["authority"]="other";break;
            }
            Assert.That(()=>VictoryDefinitionLoader.LoadSubset(j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Duplicate_keys_null_and_wrong_roots_fail_closed()
        {
            var duplicate=Source().Replace("\"initial_signed_vp\": 0","\"initial_signed_vp\": 0, \"initial_signed_vp\": 1");
            Assert.That(duplicate,Is.Not.EqualTo(Source()));
            foreach(var input in new[]{duplicate,null,"[]","{}"})
                Assert.That(()=>VictoryDefinitionLoader.LoadSubset(input),Throws.InstanceOf<ArgumentException>());
        }
    }
}
