using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class BudgetedRemovalCatalogTests
    {
        public static string Parameters => File.ReadAllText(Path.Combine(DefinitionTests.Root, "Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"));
        public static string Schema => File.ReadAllText(Path.Combine(DefinitionTests.Root, "Docs/Contracts/early-influence-batch2.schema.json"));
        // This is an ID reference set for test loading, not an assertion that these handlers are implemented.
        public static IReadOnlyCollection<string> KnownEffects => JObject.Parse(File.ReadAllText(Path.Combine(DefinitionTests.Root,
            "Data/Cards/deluxe-2015/cards.json"))).SelectTokens("$..effect_id").Values<string>().Distinct().ToArray();
        public static IReadOnlyDictionary<string, InfluenceEffectDefinition> Load() => BudgetedRemovalCatalogLoader.Load(Parameters, Schema, DefinitionTests.World(), KnownEffects);

        [Test] public void Existing_parameters_load_two_planners_and_preserve_the_prohibition_reference()
        {
            IReadOnlyDictionary<string, InfluenceEffectDefinition> catalog = null;
            Assert.DoesNotThrow(() => catalog = Load());
            Assert.That(catalog.Count, Is.EqualTo(2)); Assert.That(catalog.ContainsKey("card.east_european_unrest"), Is.False);
            var effect = (BudgetedRemovalEffect)catalog["card.socialist_governments"];
            Assert.That(effect.BlockedByEffectId, Is.EqualTo("effect.the_iron_lady"));
            var state = BudgetedRemovalTests.State(("austria", 2, 0), ("finland", 2, 0));
            var query = InfluenceEventRules.Query(state, effect, InfluenceEventPolicy.NoActiveInterference);
            Assert.That(query.RequiredAmount, Is.EqualTo(3));
            Assert.That(query.Capacities.Select(c => c.Amount), Is.EqualTo(new[] { 2, 2 }));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, InfluenceEffectDefinition>)catalog).Clear());
        }

        [Test] public void Suez_uses_loaded_scope_caps_and_the_confirmed_shortage_rule()
        {
            IReadOnlyDictionary<string, InfluenceEffectDefinition> catalog = null;
            Assert.DoesNotThrow(() => catalog = Load());
            var state = BudgetedRemovalTests.State(("france", 3, 0), ("uk", 1, 0), ("israel", 0, 1), ("egypt", 8, 0));
            var selection = InfluenceEventSelection.Allocate(PlayerSide.USSR, 0,
                new[] { BudgetedRemovalTests.A("france", 2), BudgetedRemovalTests.A("uk", 1) });
            var plan = InfluenceEventRules.Plan(state, catalog["card.suez_crisis"], selection, InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Resolved, Is.True);
            Assert.That(plan.NextState.GetInfluence(BudgetedRemovalTests.Id("france"), PlayerSide.US), Is.EqualTo(1));
            Assert.That(plan.NextState.GetInfluence(BudgetedRemovalTests.Id("egypt"), PlayerSide.US), Is.EqualTo(8));
        }

        [TestCase("version")] [TestCase("scope")] [TestCase("ruleset")] [TestCase("schema")]
        [TestCase("wrongCard")] [TestCase("wrongEffect")] [TestCase("unknownTemplate")] [TestCase("unknownCountry")]
        [TestCase("unknownRegion")] [TestCase("duplicateCountry")] [TestCase("bothScopes")]
        [TestCase("unknownBlock")] [TestCase("pendingSource")] [TestCase("unknownSource")] [TestCase("duplicateSource")]
        [TestCase("missingParameter")] [TestCase("unknownField")] [TestCase("shortagePolicy")]
        [TestCase("fractional")] [TestCase("overflow")] [TestCase("zero")] [TestCase("nullKnownEffects")]
        public void Invalid_catalog_does_not_silently_load_a_different_ruleset(string kind)
        {
            var root = JObject.Parse(Parameters); var parameters = root["parameters"];
            var suez = parameters["suez_crisis"]; var socialist = parameters["socialist_governments"];
            var schema = Schema; var known = KnownEffects;
            switch (kind)
            {
                case "version": root["content_version"] = "9.0.0"; break;
                case "scope": root["scope"] = "fixture"; break;
                case "ruleset": root["ruleset_id"] = "ruleset.other"; break;
                case "schema": schema = schema.Replace("urn:coldwarheat:contract:early-influence-batch2:1", "urn:coldwarheat:other"); break;
                case "wrongCard": suez["card_id"] = "card.fidel"; break;
                case "wrongEffect": suez["effect_id"] = "effect.fidel"; break;
                case "unknownTemplate": suez["template"] = "RunScript"; break;
                case "unknownCountry": suez["target_scope"]["country_ids"][0] = "country.imaginary"; break;
                case "unknownRegion": socialist["target_scope"]["region_id"] = "region.imaginary"; break;
                case "duplicateCountry": suez["target_scope"]["country_ids"][0] = "country.uk"; break;
                case "bothScopes": suez["target_scope"]["region_id"] = "region.europe"; break;
                case "unknownBlock": socialist["blocked_by_effect_id"] = "effect.imaginary"; break;
                case "pendingSource": suez["source_refs"][0]["verification"] = "Pending"; break;
                case "unknownSource": suez["source_refs"][0]["source_id"] = "imaginary"; break;
                case "duplicateSource": ((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone()); break;
                case "missingParameter": ((JObject)parameters).Remove("socialist_governments"); break;
                case "unknownField": suez["execute"] = "anything"; break;
                case "shortagePolicy": suez["shortage_policy"] = "Optional"; break;
                case "fractional": suez["total_amount"] = 3.5; break;
                case "overflow": suez["total_amount"] = 2147483648L; break;
                case "zero": suez["per_country_cap"] = 0; break;
                case "nullKnownEffects": known = null; break;
            }
            Assert.Catch<ArgumentException>(() => BudgetedRemovalCatalogLoader.Load(root.ToString(), schema, DefinitionTests.World(), known));
        }
    }
}
