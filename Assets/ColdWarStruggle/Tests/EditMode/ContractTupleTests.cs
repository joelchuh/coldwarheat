using System;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ContractTupleTests
    {
        static void Check(string value,string schema)=>ContractJson.Validate(ContractJson.Parse(value),(JObject)ContractJson.Parse(schema));
        const string Tuple="{\"type\":\"array\",\"prefixItems\":[{\"const\":\"first\"},{\"const\":\"second\"}],\"items\":false}";
        [Test] public void Prefix_order_is_validated_and_false_items_forbids_only_extra_tail()
        {Check("[\"first\",\"second\"]",Tuple);Check("[]",Tuple);Check("[\"first\"]",Tuple);Assert.Throws<ArgumentException>(()=>Check("[\"second\",\"first\"]",Tuple));Assert.Throws<ArgumentException>(()=>Check("[\"first\",\"second\",1]",Tuple));}
        [Test] public void Tail_schema_does_not_revalidate_prefix_and_true_items_allows_extra()
        {var s="{\"prefixItems\":[{\"type\":\"string\"}],\"items\":{\"type\":\"integer\"}}";Check("[\"head\",1,2]",s);Assert.Throws<ArgumentException>(()=>Check("[\"head\",\"bad\"]",s));Check("[\"head\",{}]",s.Replace("{\"type\":\"integer\"}","true"));Check("[\"head\",{}]","{\"prefixItems\":[{\"type\":\"string\"}]}");}
        [Test] public void Boolean_items_without_prefix_and_explicit_minimum_keep_standard_semantics()
        {Check("[]","{\"items\":false}");Assert.Throws<ArgumentException>(()=>Check("[1]","{\"items\":false}"));Check("[1]","{\"items\":true}");Assert.Throws<ArgumentException>(()=>Check("[]",Tuple.Replace("\"type\":\"array\"","\"type\":\"array\",\"minItems\":2")));}
        [Test] public void Invalid_or_recursive_prefix_contracts_fail_even_when_data_empty()
        {foreach(var s in new[]{"{\"prefixItems\":{}}","{\"prefixItems\":[]}","{\"prefixItems\":[false]}","{\"prefixItems\":[{\"unsupported\":1}]}","{\"items\":1}","{\"$defs\":{\"loop\":{\"prefixItems\":[{\"$ref\":\"#/$defs/loop\"}]}},\"$ref\":\"#/$defs/loop\"}"})Assert.Throws<ArgumentException>(()=>Check("[]",s));}
    }
}
