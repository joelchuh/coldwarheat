using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class LocalizedNamesTests
    {
        internal static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Localization/zh-CN.catalog-names.json"));
        [Test] public void Complete_pack_matches_english_keys_and_projects_read_only_stable_ids()
        {
            var data=DealtDemoCatalogTests.Catalog();var zh=CatalogNamesLoader.Load(Source(),"zh-CN",data.Names.Keys);
            Assert.That(zh.Count,Is.EqualTo(187));Assert.That(zh["country.iran"],Is.EqualTo("伊朗"));Assert.That(zh["card.containment"],Is.EqualTo("遏制政策"));
            Assert.That(((JObject)JObject.Parse(Source())["entries"]).Properties().Select(p=>p.Name),Is.EquivalentTo(((JObject)JObject.Parse(DealtDemoCatalogTests.Names)["entries"]).Properties().Select(p=>p.Name)));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,string>)zh).Add("country.other","其他"));
            var en=CatalogNamesLoader.Load(DealtDemoCatalogTests.Names,"en-US",data.Names.Keys);Assert.That(en,Is.EquivalentTo(data.Names));
        }
        [TestCase("schema")][TestCase("dataset")][TestCase("version")][TestCase("locale")][TestCase("extra")][TestCase("blank")][TestCase("long")][TestCase("type")][TestCase("missing")][TestCase("key")][TestCase("control")]
        public void Invalid_pack_is_rejected(string kind)
        {
            var j=JObject.Parse(Source());var e=(JObject)j["entries"];
            switch(kind){case "schema":j["schema_version"]=2;break;case "dataset":j["dataset_id"]="rules.fake";break;case "version":j["content_version"]="2";break;case "locale":j["locale"]="en-US";break;case "extra":j["code"]="x";break;case "blank":e["country.iran.name"]=" ";break;case "long":e["country.iran.name"]=new string('a',161);break;case "type":e["country.iran.name"]=1;break;case "missing":e.Remove("country.iran.name");break;case "key":e["arbitrary.key"]="未知";break;case "control":e["country.iran.name"]="伊\n朗";break;}
            Assert.Throws<ArgumentException>(()=>CatalogNamesLoader.Load(j.ToString(),"zh-CN",new[]{"country.iran"}));
        }
        [Test] public void Duplicate_json_and_missing_or_ambiguous_requests_fail()
        {
            Assert.Throws<ArgumentException>(()=>CatalogNamesLoader.Load(Source().Replace("\"entries\": {","\"entries\": {\"country.iran.name\":\"duplicate\","),"zh-CN",new[]{"country.iran"}));
            foreach(var ids in new[]{null,Array.Empty<string>(),new[]{"country.iran","country.iran"},new[]{"unknown.id"},new[]{" "}})Assert.Throws<ArgumentException>(()=>CatalogNamesLoader.Load(Source(),"zh-CN",ids));
            Assert.Throws<ArgumentException>(()=>CatalogNamesLoader.Load(Source(),"fr-FR",new[]{"country.iran"}));
        }
        [Test] public void View_switch_changes_only_names_and_preserves_selection_and_snapshot()
        {
            var data=NextTurnSessionTests.Catalog();var names=CatalogNamesLoader.Load(Source(),"zh-CN",data.Names.Keys);var controller=new DemoController(data.CreateHost);
            var go=new GameObject("localization-test");try
            {
                var view=go.AddComponent<DemoView>();var scenario=data.Scenarios.Single(s=>s.Id==QuickStartDemoTests.Id);
                view.Initialize(controller,names,new[]{new DemoScenarioOption(scenario.Id,scenario.Title,scenario.Actor,scenario.SelectedCountry)},printedCards:data.PrintedCards,alternateNames:data.Names);
                var before=controller.ReadView();var card=new CardId(before.OwnHand.First());controller.SelectCard(card);var country=controller.SelectedCountry;
                Assert.That(view.DisplayNames["country.iran"],Is.EqualTo("伊朗"));Assert.That(view.SwitchNameLanguage(),Is.True);Assert.That(view.DisplayNames["country.iran"],Is.EqualTo("Iran"));
                Assert.That(controller.SelectedCard,Is.EqualTo(card));Assert.That(controller.SelectedCountry,Is.EqualTo(country));Assert.That(controller.ReadView().GameId,Is.EqualTo(before.GameId));Assert.That(controller.ReadView().Version,Is.EqualTo(before.Version));Assert.That(controller.ReadView().OwnHand,Is.EqualTo(before.OwnHand));
                Assert.That(view.SwitchNameLanguage(),Is.True);Assert.That(view.DisplayNames["country.iran"],Is.EqualTo("伊朗"));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void Legacy_view_without_alternate_names_does_not_offer_switch()
        {
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var go=new GameObject("legacy-name-test");try
            {
                var s=data.Scenarios[0];var view=go.AddComponent<DemoView>();view.Initialize(c,data.Names,new[]{new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)});var before=c.ReadView();
                Assert.That(view.SwitchNameLanguage(),Is.False);Assert.That(c.ReadView().GameId,Is.EqualTo(before.GameId));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void Hand_translation_preserves_cards_values_and_private_projection()
        {
            var data=DealtDemoCatalogTests.Catalog();var host=data.CreateHost(DealtDemoCatalogTests.ScenarioId);var snapshot=host.UsSession.ReadView();
            var zh=CatalogNamesLoader.Load(Source(),"zh-CN",data.Names.Keys);var en=HandPanelFrame.Create(snapshot,data.PrintedCards,data.Names);var cn=HandPanelFrame.Create(snapshot,data.PrintedCards,zh);
            Assert.That(cn.Rows.Count,Is.EqualTo(1));Assert.That(cn.Rows[0].Cards.Select(c=>c.Id),Is.EqualTo(en.Rows[0].Cards.Select(c=>c.Id)));Assert.That(cn.Rows[0].Cards.Select(c=>c.PrintedOps),Is.EqualTo(en.Rows[0].Cards.Select(c=>c.PrintedOps)));
            Assert.That(cn.Rows[0].Cards.All(c=>c.Title==zh[c.Id.Value]),Is.True);Assert.That(host.UsSession.ReadView().Version,Is.EqualTo(snapshot.Version));
        }
    }
}
