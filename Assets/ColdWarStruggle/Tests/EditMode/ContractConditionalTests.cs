using System;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ContractConditionalTests
    {
        [Test] public void Maximum_is_inclusive_and_combines_with_minimum_and_type()
        {
            var s=JObject.Parse("{\"type\":\"integer\",\"minimum\":1,\"maximum\":4}");
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue(1),s));Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue(4),s));
            foreach(var v in new JToken[]{new JValue(0),new JValue(5),new JValue(2.5),new JValue("2")})Assert.Throws<ArgumentException>(()=>ContractJson.Validate(v,s));
        }
        [Test] public void All_of_requires_every_constraint_and_preserves_siblings()
        {
            var s=JObject.Parse("{\"allOf\":[{\"type\":\"integer\"},{\"minimum\":1},{\"maximum\":4}],\"enum\":[1,3]}");
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue(3),s));
            foreach(var v in new[]{0,2,5})Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue(v),s));
        }
        [Test] public void Condition_uses_then_or_else_without_ignoring_outer_constraints()
        {
            var s=JObject.Parse("{\"type\":\"object\",\"required\":[\"kind\",\"ops\"],\"if\":{\"properties\":{\"kind\":{\"const\":\"score\"}}},\"then\":{\"properties\":{\"ops\":{\"type\":\"null\"}}},\"else\":{\"properties\":{\"ops\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":4}}}}");
            foreach(var json in new[]{"{\"kind\":\"score\",\"ops\":null}","{\"kind\":\"event\",\"ops\":2}"})Assert.DoesNotThrow(()=>ContractJson.Validate(JObject.Parse(json),s));
            foreach(var json in new[]{"{\"kind\":\"score\",\"ops\":2}","{\"kind\":\"event\",\"ops\":null}","{\"ops\":null}","{\"kind\":\"event\",\"ops\":5}"})Assert.Throws<ArgumentException>(()=>ContractJson.Validate(JObject.Parse(json),s));
        }
        [Test] public void A_missing_branch_is_unconstrained_and_standalone_then_else_are_ignored()
        {
            foreach(var s in new[]{"{\"if\":{\"type\":\"string\"},\"then\":{\"minLength\":3}}","{\"then\":{\"type\":\"string\"},\"else\":{\"type\":\"string\"}}"})
                Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue(2),JObject.Parse(s)));
        }
        [TestCase("{\"if\":true}")] [TestCase("{\"then\":false}")] [TestCase("{\"else\":[]}")]
        [TestCase("{\"allOf\":[]}")] [TestCase("{\"allOf\":[true]}")] [TestCase("{\"maximum\":\"4\"}")]
        [TestCase("{\"if\":{\"type\":\"string\"},\"else\":{\"execute\":\"unknown\"}}")]
        [TestCase("{\"if\":{\"type\":\"integer\"},\"then\":{\"$ref\":\"https://invalid.example/schema\"}}")]
        [TestCase("{\"$defs\":{\"loop\":{\"if\":{\"type\":\"integer\"},\"then\":{\"$ref\":\"#/$defs/loop\"}}},\"$ref\":\"#/$defs/loop\"}")]
        public void Invalid_shapes_and_unselected_branch_features_are_rejected(string schema)
            =>Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("valid"),JObject.Parse(schema)));
        [Test] public void Complexity_limit_in_if_cannot_be_interpreted_as_false()
        {
            var values=new JArray();for(int i=0;i<100010;i++)values.Add(1);
            var s=JObject.Parse("{\"if\":{\"type\":\"array\",\"items\":{\"type\":\"integer\"}},\"else\":{}}");
            var error=Assert.Catch<ArgumentException>(()=>ContractJson.Validate(values,s));Assert.That(error.Message,Is.EqualTo("Contract complexity limit."));
        }
    }
}
