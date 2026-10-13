using System;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class ContractReferenceTests
    {
        [Test] public void One_of_requires_exactly_one_match_and_keeps_sibling_constraints()
        {
            var schema=JObject.Parse("{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}],\"type\":\"string\",\"minLength\":3}");
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue("valid"),schema));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue(true),schema));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue(3),schema));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("x"),schema));
            var overlap=JObject.Parse("{\"oneOf\":[{\"type\":\"integer\"},{\"type\":\"integer\",\"minimum\":2}]}");
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue(1),overlap));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue(2),overlap));
        }
        [TestCase("{\"oneOf\":true}")]
        [TestCase("{\"oneOf\":[]}")]
        [TestCase("{\"oneOf\":[{\"type\":\"string\"},{\"execute\":\"anything\"}]}")]
        [TestCase("{\"$defs\":{\"loop\":{\"oneOf\":[{\"type\":\"string\"},{\"$ref\":\"#/$defs/loop\"}]}},\"$ref\":\"#/$defs/loop\"}")]
        public void Invalid_one_of_is_rejected_even_with_a_matching_alternative(string json)
            => Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("text"),JObject.Parse(json)));
        [TestCase("{\"type\":\"array\",\"items\":false}")]
        [TestCase("{\"anyOf\":true}")]
        [TestCase("{\"type\":[\"string\",\"unsupported\"]}")]
        [TestCase("{\"properties\":[]}")]
        [TestCase("{\"required\":\"field\"}")]
        [TestCase("{\"additionalProperties\":{}}")]
        [TestCase("{\"enum\":[]}")]
        [TestCase("{\"minItems\":-1}")]
        public void Unsupported_keyword_shapes_fail_before_matching_any_value(string json)
            => Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("text"),JObject.Parse(json)));

        [Test] public void Local_definition_reference_validates_the_target_and_sibling_constraints()
        {
            var schema=JObject.Parse("{\"$defs\":{\"name\":{\"type\":\"string\"}},\"$ref\":\"#/$defs/name\",\"minLength\":3}");
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue("valid"),schema));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("x"),schema));
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue(3),schema));
        }
        [Test] public void Any_of_does_not_skip_sibling_constraints()
        {
            var schema=JObject.Parse("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}],\"type\":\"string\",\"minLength\":3}");
            Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("x"),schema));
            Assert.DoesNotThrow(()=>ContractJson.Validate(new JValue("valid"),schema));
        }
        [TestCase("{\"$ref\":\"https://invalid.example/schema\"}")]
        [TestCase("{\"$defs\":{\"loop\":{\"$ref\":\"#/$defs/loop\"}},\"$ref\":\"#/$defs/loop\"}")]
        [TestCase("{\"$ref\":\"#/$defs/missing\"}")]
        [TestCase("{\"anyOf\":[{\"type\":\"string\"},{\"execute\":\"anything\"}]}")]
        public void Unsupported_remote_cyclic_or_hidden_schema_features_fail_closed(string json)
            => Assert.Throws<ArgumentException>(()=>ContractJson.Validate(new JValue("text"),JObject.Parse(json)));
    }
}
