using System;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class SpaceRaceDefinitionLoader
    {
        // These are profile identities, not a second table of business values.
        static readonly string[] StepIds = { "space.earth_satellite", "space.animal_in_space", "space.man_in_space", "space.man_in_earth_orbit", "space.lunar_orbit", "space.moon_landing", "space.space_shuttle", "space.space_station" };
        static readonly string[] AbilityIds = { "ability.space.two_attempts", "ability.space.headline_after_opponent", "ability.space.discard_held_card", "ability.space.eight_action_rounds" };
        static readonly SpaceAbilityKind[] AbilityKinds = { SpaceAbilityKind.AttemptLimit, SpaceAbilityKind.HeadlineSelectionPolicy, SpaceAbilityKind.EndTurnDiscardMaximum, SpaceAbilityKind.ActionRoundAllowance };
        public static SpaceRaceDefinition LoadSubset(string json)
        {
            var root = Fields(ContractJson.Parse(json), "schema_version", "id", "status", "description", "sources", "initial_position", "base_attempts_per_turn", "ordinary_advance_steps", "die", "attempt_reset_boundary", "spaces", "abilities", "ability_active_rule", "formal_card_catalog_status", "content_version");
            if (Integer(root["schema_version"]) != 1 || Text(root["id"]) != "rules.deluxe2015.space-race" || Text(root["status"]) != "verified-subset"
                || Text(root["content_version"]) != "0.2.0" || Integer(root["ordinary_advance_steps"]) != 1
                || Text(root["attempt_reset_boundary"]) != "TurnStartBeforeHeadline"
                || Text(root["ability_active_rule"]) != "OwnerAtOrBeyondMilestoneAndOpponentBelowMilestone"
                || Text(root["formal_card_catalog_status"]) != "verified-definitions-effects-pending")
                throw new ArgumentException("Unsupported space race profile or policy.");
            Text(root["description"]); ValidateSources(root["sources"]);
            var die = Fields(root["die"], "minimum", "maximum");
            var abilities = ArrayOf(root["abilities"], AbilityIds.Length).Select(token =>
            {
                var a = Fields(token, "id", "kind", "value");
                var id = Text(a["id"]); var index = Array.IndexOf(AbilityIds, id);
                if (index < 0 || Text(a["kind"]) != AbilityKinds[index].ToString()) throw new ArgumentException("Unsupported space ability identity or kind.");
                return AbilityKinds[index] == SpaceAbilityKind.HeadlineSelectionPolicy
                    ? new SpaceAbilityDefinition(id, AbilityKinds[index], headlinePolicy: Text(a["value"]))
                    : new SpaceAbilityDefinition(id, AbilityKinds[index], Integer(a["value"]));
            }).ToArray();
            var spaces = ArrayOf(root["spaces"], StepIds.Length).Select((token, index) =>
            {
                var s = Fields(token, "id", "position", "min_effective_ops", "success_roll", "vp", "ability_id");
                if (Text(s["id"]) != StepIds[index]) throw new ArgumentException("Unsupported or reordered space identity.");
                var expectedAbility = index % 2 == 1 ? AbilityIds[index / 2] : null;
                var abilityId = s["ability_id"].Type == JTokenType.Null ? null : Text(s["ability_id"]);
                if (abilityId != expectedAbility) throw new ArgumentException("Ability bound to the wrong milestone.");
                var success = Fields(s["success_roll"], "min", "max"); var vp = Fields(s["vp"], "first", "second");
                return new SpaceStepDefinition(Text(s["id"]), Integer(s["position"]), Integer(s["min_effective_ops"]), Integer(success["min"]), Integer(success["max"]), Integer(vp["first"]), Integer(vp["second"]), abilityId);
            }).ToArray();
            return new SpaceRaceDefinition(Integer(root["initial_position"]), Integer(root["base_attempts_per_turn"]), Integer(die["minimum"]), Integer(die["maximum"]), spaces, abilities);
        }
        static void ValidateSources(JToken token)
        {
            var sources = ArrayOf(token, 2);
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var item in sources)
            {
                if (item is not JObject candidate) throw new ArgumentException("Invalid space source.");
                var id = Text(candidate["id"]);
                if (!seen.Add(id)) throw new ArgumentException("Duplicate space source.");
                JObject source;
                if (id == "R2015")
                {
                    source = Fields(item, "id", "url", "checked_on", "sections", "pdf_pages");
                    if (Text(source["url"]) != "https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"
                        || !ArrayOf(source["sections"], 3).Select(Text).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "3.4", "6.4", "7.4.2" })
                        || !ArrayOf(source["pdf_pages"], 3).Select(Integer).OrderBy(x => x).SequenceEqual(new[] { 4, 7, 8 }))
                        throw new ArgumentException("Unsupported rule citation.");
                }
                else if (id == "M-DELUXE")
                {
                    source = Fields(item, "id", "url", "listed_by", "scope", "checked_on");
                    if (Text(source["url"]) != "https://s3-us-west-2.amazonaws.com/gmtwebsiteassets/nnts/TSMap11.jpg"
                        || Text(source["listed_by"]) != "https://www.gmtgames.com/p-927-twilight-struggle-deluxe-edition-8th-printing.aspx")
                        throw new ArgumentException("Unsupported map citation.");
                    Text(source["scope"]);
                }
                else throw new ArgumentException("Unknown space source.");
                if (!DateTime.TryParseExact(Text(source["checked_on"]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    throw new ArgumentException("Invalid source date.");
            }
        }
        static JObject Fields(JToken token, params string[] expected)
        {
            if (token is not JObject obj || obj.Count != expected.Length || obj.Properties().Any(p => !expected.Contains(p.Name)))
                throw new ArgumentException("Unexpected space race fields.");
            return obj;
        }
        static JArray ArrayOf(JToken token, int length)
        {
            if (token is not JArray array || array.Count != length) throw new ArgumentException("Invalid space array size.");
            return array;
        }
        static string Text(JToken token)
        {
            if (token?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)token)) throw new ArgumentException("Nonempty space text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if (token?.Type != JTokenType.Integer || !int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                throw new ArgumentException("Space integer wrong type or out of range.");
            return number;
        }
    }
}
