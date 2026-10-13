using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class RandomSnapshotJsonTests
    {
        const string Valid="{\"schema_version\":1,\"algorithm_id\":\"pcg32-xsh-rr-v1\",\"state\":\"0000000000000000\",\"increment\":\"0000000000000001\",\"draw_count\":\"0000000000000000\"}";
        [Test] public void Encoding_is_canonical_and_uses_strings_for_all_64_bit_values()
        {
            var snapshot=new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,0,1,0);
            Assert.That(RandomSnapshotJson.Serialize(snapshot),Is.EqualTo(Valid));
            Pcg32RandomTests.Same(snapshot,RandomSnapshotJson.Deserialize(Valid));
        }
        [Test] public void All_unsigned_maximum_values_survive_round_trip_without_float_precision_loss()
        {
            var snapshot=new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,ulong.MaxValue,ulong.MaxValue,ulong.MaxValue);
            var json=RandomSnapshotJson.Serialize(snapshot);
            var parsed=JObject.Parse(json);
            foreach(var field in new[]{"state","increment","draw_count"})Assert.That((string)parsed[field],Is.EqualTo("ffffffffffffffff"));
            Pcg32RandomTests.Same(snapshot,RandomSnapshotJson.Deserialize(json));
        }
        [Test] public void Serialized_midstream_state_resumes_dice_results()
        {
            var rng=Pcg32Random.FromSeed(100,23);for(int i=0;i<10;i++)rng.NextInt(1,7);
            var restored=new Pcg32Random(RandomSnapshotJson.Deserialize(RandomSnapshotJson.Serialize(rng.Capture())));
            for(int i=0;i<64;i++)Assert.That(restored.NextInt(1,7),Is.EqualTo(rng.NextInt(1,7)));
            Pcg32RandomTests.Same(rng.Capture(),restored.Capture());
        }
        [TestCase("schema_version","2")] [TestCase("schema_version","\"1\"")] [TestCase("schema_version","1.0")]
        [TestCase("algorithm_id","\"future\"")] [TestCase("algorithm_id","null")]
        [TestCase("state","123")] [TestCase("state","\"0\"")] [TestCase("state","\"FFFFFFFFFFFFFFFF\"")]
        [TestCase("state","\"000000000000000g\"")] [TestCase("state","\"0000000000000000\\n\"")]
        [TestCase("state","\" 000000000000000\"")] [TestCase("state","\"00000000000000000\"")]
        [TestCase("increment","\"0000000000000000\"")] [TestCase("increment","\"0000000000000002\"")]
        [TestCase("draw_count","-1")] [TestCase("draw_count","\"-000000000000001\"")]
        public void Invalid_field_is_rejected(string field,string raw)
        {
            var value=JObject.Parse(Valid);value[field]=JToken.Parse(raw);
            Assert.That(()=>RandomSnapshotJson.Deserialize(value.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("schema_version")] [TestCase("algorithm_id")] [TestCase("state")] [TestCase("increment")] [TestCase("draw_count")]
        public void Every_field_is_required(string field)
        {
            var value=JObject.Parse(Valid);value.Remove(field);
            Assert.That(()=>RandomSnapshotJson.Deserialize(value.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Unknown_fields_duplicate_fields_trailing_documents_and_nonobjects_are_rejected()
        {
            foreach(var text in new[]{Valid.Replace("{","{\"future\":0,"),Valid.Replace("{","{\"state\":\"0000000000000001\","),Valid+"{}","[]","null","",null})
                Assert.That(()=>RandomSnapshotJson.Deserialize(text),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>RandomSnapshotJson.Serialize(null),Throws.InstanceOf<ArgumentException>());
        }
    }
}
