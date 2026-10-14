using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class DefconVpEventLoader
    {
        public static IReadOnlyList<DefconVpEffectDefinition> LoadImmediateSubset(string json,string schema)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:defcon-vp-events:1")throw new ArgumentException("Unsupported DEFCON event contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.defcon_vp_events" || (string)root["ruleset_id"]!="ruleset.deluxe2015"
                || (string)root["content_version"]!="0.1.0" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported event profile.");
            var sources=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])if(!sources.Add((string)source["id"]))throw new ArgumentException("Duplicate source.");
            foreach(var property in ((JObject)root["parameters"]).Properties())
            {
                var p=property.Value;
                if((string)p["card_id"]!="card."+property.Name || (string)p["effect_id"]!="effect."+property.Name)throw new ArgumentException("Event identity mismatch.");
                foreach(var source in p["source_refs"])if((string)source["verification"]!="Verified" || !sources.Contains((string)source["source_id"]))throw new ArgumentException("Unverified source.");
            }
            DefconVpEffectDefinition Read(string name,DefconVpEventKind kind,string owner,string formula,string first,string second)
            {
                var p=root["parameters"][name];
                if((string)p["template"]!="OrderedDefconVpEvent" || (string)p["vp_owner"]!=owner || (string)p["vp_formula"]!=formula
                    || !p["steps"].Values<string>().SequenceEqual(new[]{first,second}) || (string)p["vp_checkpoint"]!="VpBatchClosed"
                    || (string)p["defcon_ref"]["dataset_id"]!="rules.deluxe2015.defcon" || (string)p["defcon_ref"]["json_pointer"]!="")throw new ArgumentException("Unsupported event semantics.");
                return new DefconVpEffectDefinition(new CardId((string)p["card_id"]),(string)p["effect_id"],kind,(int)p["defcon_change"],(int)p["vp_constant"]);
            }
            // The third schema-checked record is deliberately not executable until its choice flow exists.
            return Array.AsReadOnly(new[]{Read("duck_and_cover",DefconVpEventKind.DegradeThenUsAward,"US","FixedConstantMinusCurrentDefcon","DegradeDefcon","AwardVpFromResultingDefcon"),
                Read("nuclear_test_ban",DefconVpEventKind.ControllerAwardThenImprove,"EffectController","CurrentDefconMinusFixedConstant","AwardVpFromCurrentDefcon","ImproveDefcon")});
        }
    }
}
