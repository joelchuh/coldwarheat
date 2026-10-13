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
    public class TurnCalendarTests
    {
        public static string Source()=>File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../Data/Rulesets/deluxe-2015/turn-flow.json"));
        public static TurnCalendar Calendar()=>TurnCalendarLoader.Load(Source());
        [TestCase(1,GameEra.Early,8,6)] [TestCase(3,GameEra.Early,8,6)]
        [TestCase(4,GameEra.Mid,9,7)] [TestCase(7,GameEra.Mid,9,7)]
        [TestCase(8,GameEra.Late,9,7)] [TestCase(10,GameEra.Late,9,7)]
        public void Actual_calendar_uses_shared_era_boundaries(int number,GameEra era,int hand,int actions)
        {
            var calendar=Calendar();var turn=calendar.ForTurn(number);
            Assert.That(turn.Number,Is.EqualTo(number));Assert.That(turn.Era,Is.EqualTo(era));
            Assert.That(turn.Parameters.HandTarget,Is.EqualTo(hand));Assert.That(turn.Parameters.NormalActionsPerSide,Is.EqualTo(actions));
            Assert.That(turn.Parameters,Is.SameAs(calendar.Bands.Single(b=>b.Era==era)));Assert.That(calendar.LastTurn,Is.EqualTo(10));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(11)] [TestCase(int.MaxValue)]
        public void Out_of_calendar_turn_is_rejected_without_defaulting(int number)
        { var calendar=Calendar();Assert.Throws<ArgumentOutOfRangeException>(()=>calendar.ForTurn(number)); }

        [Test] public void Calendar_copies_bands_and_exposes_no_mutable_collection()
        {
            var original=Calendar();var bands=original.Bands.ToArray();var calendar=new TurnCalendar(bands.Reverse());
            bands[0]=null;Assert.That(calendar.Bands[0].Era,Is.EqualTo(GameEra.Early));
            Assert.Throws<NotSupportedException>(()=>((IList<EraTurnParameters>)calendar.Bands).Clear());
        }
        [TestCase("null")] [TestCase("empty")] [TestCase("missing")] [TestCase("duplicate")]
        [TestCase("null-band")] [TestCase("gap")] [TestCase("overlap")] [TestCase("wrong-order")]
        [TestCase("not-first-turn")]
        public void Calendar_rejects_incomplete_or_ambiguous_bands(string kind)
        {
            var bands=Calendar().Bands.ToArray();IEnumerable<EraTurnParameters> input=bands;
            if(kind=="null")input=null;if(kind=="empty")input=Array.Empty<EraTurnParameters>();
            if(kind=="missing")input=bands.Take(2);if(kind=="duplicate")input=bands.Concat(new[]{bands[0]});
            if(kind=="null-band")bands[1]=null;
            if(kind=="gap")bands[1]=new EraTurnParameters(GameEra.Mid,5,7,9,7);
            if(kind=="overlap")bands[1]=new EraTurnParameters(GameEra.Mid,3,7,9,7);
            if(kind=="wrong-order")bands[1]=new EraTurnParameters(GameEra.Mid,8,10,9,7);
            if(kind=="not-first-turn")bands[0]=new EraTurnParameters(GameEra.Early,2,3,8,6);
            Assert.That(()=>new TurnCalendar(input),Throws.ArgumentException);
        }
        [TestCase(-1,1,3,8,6)] [TestCase(0,0,3,8,6)] [TestCase(0,4,3,8,6)]
        [TestCase(0,1,3,0,6)] [TestCase(0,1,3,8,0)]
        public void Invalid_era_parameters_are_rejected(int era,int first,int last,int hand,int actions)
        { Assert.Throws<ArgumentOutOfRangeException>(()=>new EraTurnParameters((GameEra)era,first,last,hand,actions)); }

        [TestCase("version")] [TestCase("dataset")] [TestCase("content")] [TestCase("unverified")]
        [TestCase("bands-null")] [TestCase("unknown-era")] [TestCase("decimal")]
        [TestCase("overflow")] [TestCase("missing-actions")] [TestCase("source")]
        [TestCase("gap")] [TestCase("overlap")] [TestCase("unknown-band-field")]
        public void Loader_rejects_invalid_profile_and_era_data(string kind)
        {
            var root=JObject.Parse(Source());var band=root["era_bands"][1];
            switch(kind)
            {
                case "version":root["schema_version"]=2;break;
                case "dataset":root["id"]="rules.other";break;
                case "content":root["content_version"]="future";break;
                case "unverified":root["status"]="draft";break;
                case "bands-null":root["era_bands"]=null;break;
                case "unknown-era":band["id"]="later";break;
                case "decimal":band["first_turn"]=4.5;break;
                case "overflow":band["last_turn"]=long.MaxValue;break;
                case "missing-actions":((JObject)band).Remove("normal_actions_per_side");break;
                case "source":root["source"]["id"]="unknown";break;
                case "gap":band["first_turn"]=5;break;
                case "overlap":band["first_turn"]=3;break;
                case "unknown-band-field":band["override_era"]="early";break;
            }
            Assert.That(()=>TurnCalendarLoader.Load(root.ToString()),Throws.InstanceOf<ArgumentException>());
        }
    }
}
