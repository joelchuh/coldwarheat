using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class InitialDealLoaderTests
    {
        public static InitialDealDefinition Load()=>InitialDealDefinitionLoader.LoadSubset(SetupDeploymentTests.Source(),SetupDeploymentTests.Schema(),TurnCalendarTests.Source(),CardCatalogLoaderTests.Load());
        [Test] public void Actual_setup_and_shared_parameters_create_a_complete_initial_card_partition()
        {
            var catalog=CardCatalogLoaderTests.Load();var definition=Load();
            Assert.That(definition.Turn.Number,Is.EqualTo(1));Assert.That(definition.Turn.Era,Is.EqualTo(GameEra.Early));Assert.That(definition.HandTarget,Is.EqualTo(8));
            Assert.That(definition.China.Id,Is.EqualTo(new CardId("card.china")));Assert.That(definition.China.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(definition.China.Face,Is.EqualTo(CardFace.FaceUp));
            var result=InitialDealService.Create(catalog,definition,Pcg32Random.FromSeed(42,54));
            Assert.That(result.Cards.GetHand(PlayerSide.US),Has.Count.EqualTo(8));Assert.That(result.Cards.DrawPile,Has.Count.EqualTo(19));
        }
        [Test] public void Hand_target_is_read_from_shared_parameters_instead_of_a_second_constant()
        {
            // Synthetic parameter variation checks the reference plumbing, not a new approved game rule.
            var turn=JObject.Parse(TurnCalendarTests.Source());turn["era_bands"][0]["hand_target"]=6;
            var definition=InitialDealDefinitionLoader.LoadSubset(SetupDeploymentTests.Source(),SetupDeploymentTests.Schema(),turn.ToString(),CardCatalogLoaderTests.Load());
            Assert.That(definition.HandTarget,Is.EqualTo(6));
        }
        [TestCase("schema")] [TestCase("dataset")] [TestCase("version")] [TestCase("ruleset")] [TestCase("scope")]
        [TestCase("cards")] [TestCase("era")] [TestCase("china-id")] [TestCase("china-owner")] [TestCase("china-face")]
        [TestCase("source-unknown")] [TestCase("source-pending")] [TestCase("source-whitespace")] [TestCase("source-duplicate")] [TestCase("root-source-pending")]
        [TestCase("turn-dataset")] [TestCase("turn-pointer")] [TestCase("hand-dataset")] [TestCase("hand-pointer")]
        [TestCase("turn-order")] [TestCase("turn-first")] [TestCase("turn-version")] [TestCase("hand-overflow")] [TestCase("hand-too-many")]
        [TestCase("missing")] [TestCase("extra")] [TestCase("short-deck")] [TestCase("null-catalog")]
        public void Invalid_setup_reference_provenance_and_cross_file_values_fail_closed(string defect)
        {
            var setup=JObject.Parse(SetupDeploymentTests.Source());var schema=JObject.Parse(SetupDeploymentTests.Schema());var turn=JObject.Parse(TurnCalendarTests.Source());var catalog=CardCatalogLoaderTests.Load();
            switch(defect)
            {
                case "schema":schema["$id"]="urn:unknown";break;
                case "dataset":setup["dataset_id"]="setup.other";break;
                case "version":setup["content_version"]="0.2.0";break;
                case "ruleset":setup["ruleset_id"]="ruleset.other";break;
                case "scope":setup["scope"]="fixture";break;
                case "cards":setup["cards_dataset_id"]="cards.other";break;
                case "era":setup["initial_card_era"]="Mid";break;
                case "china-id":setup["china"]["card_id"]="card.fidel";break;
                case "china-owner":setup["china"]["owner"]="US";break;
                case "china-face":setup["china"]["face"]="FaceDown";break;
                case "source-unknown":setup["china"]["source_refs"][0]["source_id"]="UNKNOWN";break;
                case "source-pending":setup["china"]["source_refs"][0]["verification"]="Pending";break;
                case "source-whitespace":setup["china"]["source_refs"][0]["locator"]=" ";break;
                case "source-duplicate":((JArray)setup["source_catalog"]).Add(setup["source_catalog"][0].DeepClone());break;
                case "root-source-pending":setup["source_refs"][0]["verification"]="Pending";break;
                case "turn-dataset":setup["initial_values"]["turn_number"]["dataset_id"]="rules.other";break;
                case "turn-pointer":setup["initial_values"]["turn_number"]["json_pointer"]="/era_bands/1/first_turn";break;
                case "hand-dataset":setup["initial_values"]["hand_target"]["dataset_id"]="rules.other";break;
                case "hand-pointer":setup["initial_values"]["hand_target"]["json_pointer"]="/era_bands/1/hand_target";break;
                case "turn-order":turn["era_bands"]=new JArray(((JArray)turn["era_bands"]).Reverse().Select(x=>x.DeepClone()));break;
                case "turn-first":turn["era_bands"][0]["first_turn"]=2;break;
                case "turn-version":turn["content_version"]="9.0.0";break;
                case "hand-overflow":turn["era_bands"][0]["hand_target"]=long.MaxValue;break;
                case "hand-too-many":turn["era_bands"][0]["hand_target"]=18;break;
                case "missing":setup.Remove("china");break;
                case "extra":setup["hand_target"]=8;break;
                case "short-deck":catalog=CardZonesTests.Catalog();break;
                case "null-catalog":catalog=null;break;
            }
            Assert.That(()=>InitialDealDefinitionLoader.LoadSubset(setup.ToString(),schema.ToString(),turn.ToString(),catalog),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Initial_definition_rejects_noninitial_turn_and_later_era()
        {
            foreach(var number in new[]{2,4})Assert.That(()=>new InitialDealDefinition(TurnCalendarTests.Calendar().ForTurn(number),CardZonesTests.China()),Throws.InstanceOf<ArgumentException>());
        }
    }
}
