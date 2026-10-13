using System;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class VictoryDefinitionLoader
    {
        // Four base checkpoints only. This does not certify final scoring, card endings or an entire RulePack.
        public static VictoryDefinition LoadSubset(string json)
        {
            var root=Fields(ContractJson.Parse(json),"schema_version","id","status","scope","sources","initial_signed_vp","positive_side",
                "negative_side","automatic_vp_threshold","final_tie_score","held_scoring_rows","excluded_answer","product_policy","project_adjudications");
            if(Integer(root["schema_version"])!=2 || Text(root["id"])!="rules.deluxe2015.victory"
                || Text(root["status"])!="verified-subset" || Text(root["positive_side"])!="US" || Text(root["negative_side"])!="USSR"
                || Integer(root["final_tie_score"])!=0
                || Text(root["excluded_answer"])!="F2010 PDF p21: military victory skips held-card audit"
                || Text(root["product_policy"])!="Private authoritative held-scoring audit in normal digital mode")
                throw new ArgumentException("Unsupported victory profile.");
            Text(root["scope"]);ValidateSources(root["sources"]);
            var inputs=Array(root["held_scoring_rows"],12);
            var rows=inputs.Select(token=>
            {
                var row=Fields(token,"case_id","us_holds","ussr_holds","military_candidate","outcome","winner","authority","adoption_status");
                var outcome=Text(row["outcome"]) switch
                {
                    "Continue"=>VictoryOutcome.Continue,"Finished"=>VictoryOutcome.Finished,
                    "UnresolvedAdjudication"=>VictoryOutcome.UnresolvedAdjudication,
                    _=>throw new ArgumentException("Unknown held-scoring outcome.")
                };
                return new HeldScoringRow(Text(row["case_id"]),Boolean(row["us_holds"]),Boolean(row["ussr_holds"]),Side(row["military_candidate"]),
                    outcome,Side(row["winner"]),Text(row["authority"]),Text(row["adoption_status"]));
            }).ToArray();
            var expectedIds=Enumerable.Range(1,12).Select(n=>"RV-H-"+n.ToString("D2",CultureInfo.InvariantCulture));
            if(!rows.Select(x=>x.Id).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expectedIds))
                throw new ArgumentException("Unexpected held-scoring case identities.");
            ValidateAdoption(root["project_adjudications"],rows);
            return new VictoryDefinition(Integer(root["initial_signed_vp"]),Integer(root["automatic_vp_threshold"]),rows);
        }
        static void ValidateSources(JToken token)
        {
            var sources=Array(token,2);
            if(sources.Any(s=>s is not JObject))throw new ArgumentException("Victory source entries must be objects.");
            if(!sources.Select(s=>Text(s["id"])).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"F2010","R2015"}))
                throw new ArgumentException("Unsupported victory sources.");
            foreach(var tokenSource in sources)
            {
                string id=Text(tokenSource["id"]);
                var source=id=="R2015"?Fields(tokenSource,"id","url","checked_on","sections"):Fields(tokenSource,"id","url","checked_on","pdf_pages","scope");
                Date(source["checked_on"]);
                if(id=="R2015")
                {
                    if(Text(source["url"])!="https://www.gmtgames.com/nnts/TS_Rules-2015.pdf")throw new ArgumentException("Unsupported victory rule source.");
                    var expected=new[]{"4.5D–F","8.1.3","10.2","10.3","11.1.2"}.OrderBy(x=>x,StringComparer.Ordinal);
                    if(!Array(source["sections"],5).Select(Text).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expected))
                        throw new ArgumentException("Unsupported victory source sections.");
                }
                else
                {
                    if(Text(source["url"])!="https://www.gmtgames.com/nnts/FAQv5.pdf" || Integer(Array(source["pdf_pages"],1)[0])!=22)
                        throw new ArgumentException("Unsupported held-scoring FAQ source.");
                    Text(source["scope"]);
                }
            }
        }
        static void ValidateAdoption(JToken token,HeldScoringRow[] rows)
        {
            var project=Fields(Array(token,1)[0],"id","case_id","status","approved_by","approved_on","decision","basis","authority_limit");
            if(Text(project["id"])!="CF-003" || Text(project["case_id"])!="RV-H-11" || Text(project["status"])!="adopted"
                || Text(project["approved_by"])!="user")throw new ArgumentException("Unadopted project interpretation.");
            Date(project["approved_on"]);Text(project["decision"]);Text(project["authority_limit"]);
            var basis=Array(project["basis"],2).Select(Text).ToArray();
            if(basis.Distinct(StringComparer.Ordinal).Count()!=2)throw new ArgumentException("Invalid project adoption basis.");
            var row=rows.Single(r=>r.Id=="RV-H-11");
            if(row.Authority!="CF-003" || row.AdoptionStatus!="adopted-project-interpretation")
                throw new ArgumentException("Missing adopted matrix linkage.");
        }
        static JObject Fields(JToken token,params string[] expected)
        {
            if(token is not JObject obj || obj.Count!=expected.Length || obj.Properties().Any(p=>!expected.Contains(p.Name)))
                throw new ArgumentException("Unexpected victory fields.");
            return obj;
        }
        static JArray Array(JToken token,int count)
        {
            if(token is not JArray array || array.Count!=count)throw new ArgumentException("Invalid victory array.");
            return array;
        }
        static string Text(JToken token)
        {
            if(token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token))throw new ArgumentException("Nonempty victory text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var number))
                throw new ArgumentException("Victory integer out of range or wrong type.");
            return number;
        }
        static bool Boolean(JToken token)
        {
            if(token?.Type!=JTokenType.Boolean)throw new ArgumentException("Victory boolean required.");
            return (bool)token;
        }
        static PlayerSide? Side(JToken token)
        {
            if(token?.Type==JTokenType.Null)return null;
            return Text(token) switch {"US"=>PlayerSide.US,"USSR"=>PlayerSide.USSR,_=>throw new ArgumentException("Unknown victory side.")};
        }
        static void Date(JToken token)
        {
            if(!DateTime.TryParseExact(Text(token),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))
                throw new ArgumentException("Invalid victory source date.");
        }
    }
}
