using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefectorsHeadlineDefinitionTests
    {
        internal static string Data()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/defectors.parameters.json"));
        internal static string Schema()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/defectors.schema.json"));
        internal static CardId Id(string n)=>HeadlineFlowTests.Id(n);
        internal static DefectorsHeadlineDefinition Definition(CardCatalog catalog)=>DefectorsHeadlineLoader.Load(Data(),Schema(),catalog);
        static HeadlineResolutionState Queue(GameState s,DefectorsHeadlineDefinition definition=null)=>HeadlineResolutionState.CreateSelected(HeadlineResolutionTests.Parameters(),s.Practice.Definition.Cards,s.Cards,s.Turn,definition??Definition(s.Practice.Definition.Cards));
        [Test] public void Canonical_loader_binds_shared_card_without_copying_printed_values()
        {
            var cards=HeadlineResolutionTests.Ready().Practice.Definition.Cards;var d=Definition(cards);
            Assert.That(d.Catalog,Is.SameAs(cards));Assert.That(d.Card,Is.EqualTo(Id("defectors")));Assert.That(cards.GetCard(d.Card).PrintedOps,Is.EqualTo(2));Assert.That(cards.GetCard(d.Card).RemoveAfterResolvedEvent,Is.False);
        }
        [TestCase("china")][TestCase("europe_scoring")][TestCase("fidel")][TestCase("containment")][TestCase("olympic_games")][TestCase("missing")]
        public void Binding_rejects_incompatible_card(string name)
        {var cards=HeadlineResolutionTests.Ready().Practice.Definition.Cards;Assert.Throws<ArgumentException>(()=>new DefectorsHeadlineDefinition(cards,Id(name)));}
        [Test] public void Missing_catalog_and_default_card_are_rejected()
        {Assert.Throws<ArgumentNullException>(()=>new DefectorsHeadlineDefinition(null,Id("defectors")));Assert.Throws<ArgumentException>(()=>new DefectorsHeadlineDefinition(HeadlineResolutionTests.Ready().Practice.Definition.Cards,default));}
        [TestCase("profile")][TestCase("strategy")][TestCase("target")][TestCase("self")][TestCase("disposition")][TestCase("success")][TestCase("unknown-field")][TestCase("contract-id")][TestCase("relaxed-contract")][TestCase("relaxed-profile")]
        public void Changed_profile_or_selected_headline_semantics_are_not_silently_accepted(string change)
        {
            var cards=HeadlineResolutionTests.Ready().Practice.Definition.Cards;var data=JObject.Parse(Data());var schema=JObject.Parse(Schema());var p=data["parameters"]["defectors"];
            switch(change)
            {
                case "profile":data["ruleset_id"]="elsewhere";break;
                case "strategy":p["headline_strategy"]="Ordinary";break;
                case "target":p["us_selected_target"]="USHeadline";break;
                case "self":p["ussr_selected"]="AwardVp";break;
                case "disposition":p["cancelled_disposition"]="RemovedCards";break;
                case "success":p["cancelled_success_fact"]=true;break;
                case "unknown-field":p["trusted"]=true;break;
                case "contract-id":schema["$id"]="untrusted";break;
                case "relaxed-contract":p["cancelled_success_fact"]=true;schema["$defs"]["defectors"]["properties"]["cancelled_success_fact"]["const"]=true;break;
                case "relaxed-profile":data["content_version"]="9";schema["properties"]["content_version"]["const"]="9";break;
            }
            Assert.Throws<ArgumentException>(()=>DefectorsHeadlineLoader.Load(data.ToString(),schema.ToString(),cards));
        }
        [Test] public void Duplicate_properties_and_trailing_document_are_rejected()
        {var c=HeadlineResolutionTests.Ready().Practice.Definition.Cards;Assert.Throws<ArgumentException>(()=>DefectorsHeadlineLoader.Load(Data()+"{}",Schema(),c));Assert.Throws<ArgumentException>(()=>DefectorsHeadlineLoader.Load(Data().Replace("\"schema_version\": 1","\"schema_version\": 1, \"schema_version\": 1"),Schema(),c));}
        [TestCase("red_scare_purge")][TestCase("europe_scoring")][TestCase("fidel")]
        public void Us_selected_defectors_has_named_priority_even_over_four_ops(string opposing)
        {
            var s=HeadlineResolutionTests.Ready("defectors",opposing);var q=Queue(s);
            Assert.That(q.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.USSelectedDefectorsFirst));Assert.That(q.Current.PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(q.Current.Card,Is.EqualTo(Id("defectors")));
            Assert.That(q.Current.Value,Is.EqualTo(s.Practice.Definition.Cards.GetCard(Id("defectors")).PrintedOps));Assert.That(q.CurrentCancellationSource,Is.Null);Assert.That(q.CompletedRoots,Is.Empty);
        }
        [TestCase("containment",PlayerSide.US)][TestCase("fidel",PlayerSide.US)][TestCase("nasser",PlayerSide.USSR)]
        public void Soviet_self_selection_does_not_gain_extra_ordering_priority(string us,PlayerSide first)
        {
            var s=HeadlineResolutionTests.Ready(us,"defectors");var q=Queue(s);Assert.That(q.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.OrdinaryPrintedValue));Assert.That(q.Current.PlayedBy,Is.EqualTo(first));
            if(first==PlayerSide.US)q=q.AdvanceAfterResolution(Id(us),PlayerSide.US);
            Assert.That(q.Current.Card,Is.EqualTo(Id("defectors")));Assert.That(q.CurrentCancellationSource,Is.EqualTo(Id("defectors")));
        }
        [Test] public void Successful_us_source_then_cancelled_soviet_root_have_distinct_immutable_facts()
        {
            var q=Queue(HeadlineResolutionTests.Ready("defectors","red_scare_purge"));Assert.Throws<ArgumentException>(()=>q.AdvanceAfterCancellation(Id("defectors"),PlayerSide.US));
            var first=q.AdvanceAfterResolution(Id("defectors"),PlayerSide.US);Assert.That(first.CurrentCancellationSource,Is.EqualTo(Id("defectors")));Assert.That(first.CompletedRoots.Single().Outcome,Is.EqualTo(HeadlineRootOutcome.Resolved));Assert.That(first.CompletedRoots[0].CancellationSource,Is.Null);
            Assert.Throws<ArgumentException>(()=>first.AdvanceAfterResolution(Id("red_scare_purge"),PlayerSide.USSR));
            var done=first.AdvanceAfterCancellation(Id("red_scare_purge"),PlayerSide.USSR);Assert.That(done.IsComplete,Is.True);Assert.That(done.CompletedCount,Is.EqualTo(2));Assert.That(done.CompletedRoots[1].Outcome,Is.EqualTo(HeadlineRootOutcome.Cancelled));Assert.That(done.CompletedRoots[1].CancellationSource,Is.EqualTo(Id("defectors")));
            Assert.That(q.CompletedRoots,Is.Empty);Assert.That(first.CompletedRoots,Has.Count.EqualTo(1));Assert.That(done.CurrentCancellationSource,Is.Null);Assert.That(done.CurrentResolutionId,Is.Null);
            Assert.Throws<NotSupportedException>(()=>((IList<HeadlineRootCompletion>)done.CompletedRoots).Clear());
        }
        [Test] public void Self_cancel_source_is_itself_and_does_not_cancel_us_independent_root()
        {
            var q=Queue(HeadlineResolutionTests.Ready("nasser","defectors"));Assert.Throws<ArgumentException>(()=>q.AdvanceAfterResolution(Id("defectors"),PlayerSide.USSR));
            var after=q.AdvanceAfterCancellation(Id("defectors"),PlayerSide.USSR);Assert.That(after.Current.PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(after.CurrentCancellationSource,Is.Null);Assert.That(after.CompletedRoots[0].Outcome,Is.EqualTo(HeadlineRootOutcome.Cancelled));
            Assert.Throws<ArgumentException>(()=>after.AdvanceAfterCancellation(Id("nasser"),PlayerSide.US));var done=after.AdvanceAfterResolution(Id("nasser"),PlayerSide.US);Assert.That(done.CompletedRoots[1].Outcome,Is.EqualTo(HeadlineRootOutcome.Resolved));
        }
        [Test] public void Cancellation_requires_exact_current_card_owner_and_uncompleted_root()
        {
            var q=Queue(HeadlineResolutionTests.Ready("defectors","fidel")).AdvanceAfterResolution(Id("defectors"),PlayerSide.US);
            Assert.Throws<ArgumentException>(()=>q.AdvanceAfterCancellation(Id("fidel"),PlayerSide.US));Assert.Throws<ArgumentException>(()=>q.AdvanceAfterCancellation(Id("nasser"),PlayerSide.USSR));
            var done=q.AdvanceAfterCancellation(Id("fidel"),PlayerSide.USSR);Assert.Throws<ArgumentException>(()=>done.AdvanceAfterCancellation(Id("fidel"),PlayerSide.USSR));Assert.Throws<ArgumentException>(()=>done.AdvanceAfterResolution(Id("fidel"),PlayerSide.USSR));
        }
        [Test] public void No_special_binding_preserves_explicit_ordinary_ordering_and_forbids_cancellation()
        {
            var s=HeadlineResolutionTests.Ready("defectors","red_scare_purge");var q=HeadlineResolutionState.CreateSelected(HeadlineResolutionTests.Parameters(),s.Practice.Definition.Cards,s.Cards,s.Turn,null);
            Assert.That(q.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.OrdinaryPrintedValue));Assert.That(q.Current.PlayedBy,Is.EqualTo(PlayerSide.USSR));Assert.That(q.CurrentCancellationSource,Is.Null);Assert.Throws<ArgumentException>(()=>q.AdvanceAfterCancellation(q.Current.Card,q.Current.PlayedBy));
            var ordinary=HeadlineResolutionState.CreateOrdinary(HeadlineResolutionTests.Parameters(),s.Practice.Definition.Cards,s.Cards,s.Turn);Assert.That(ordinary.Order.Select(x=>x.Card),Is.EqualTo(q.Order.Select(x=>x.Card)));
        }
        [Test] public void Registration_from_another_catalog_is_rejected_even_for_equal_ids()
        {var s=HeadlineResolutionTests.Ready("defectors","fidel");var foreign=Definition(HeadlineResolutionTests.Ready().Practice.Definition.Cards);Assert.Throws<ArgumentException>(()=>Queue(s,foreign));}
    }
}
