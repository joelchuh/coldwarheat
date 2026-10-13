using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ColdWarStruggle.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed class WorldMapAssets
    {
        WorldMapAssets() { }
        public IReadOnlyDictionary<string,Vector2> Anchors { get; private set; }
        public IReadOnlyList<IReadOnlyList<Vector2>> LandRings { get; private set; }
        // This loader deliberately accepts only the implemented Flat2D marker profile.
        // It is not a general JSON Schema engine or a loader for arbitrary theme assets.
        public static WorldMapAssets Load(string layoutJson,string landJson,MapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var layout=Parse(layoutJson);var land=Parse(landJson);
            Fields(layout,"schema_version","content_version","scope","description","layout_id","map_dataset_id","layout_kind","nodes","connection_routes");
            Version(layout);Text(layout,"description");
            if(!Regex.IsMatch(Text(layout,"content_version"),@"^[0-9]+\.[0-9]+\.[0-9]+$") ||
                !Regex.IsMatch(Text(layout,"layout_id"),@"^layout\.[a-z][a-z0-9_.]*$"))throw Bad();
            Equal(layout,"scope","fixture");Equal(layout,"layout_kind","Flat2D");Equal(layout,"map_dataset_id","map.deluxe2015.world");
            if(!(layout["connection_routes"] is JArray routes) || routes.Count!=0)throw Bad();
            var expected=new HashSet<string>(map.Countries.Select(c=>c.Id.Value),StringComparer.Ordinal){"superpower.us","superpower.ussr"};
            if(!(layout["nodes"] is JArray nodes) || nodes.Count!=expected.Count || nodes.Count>256)throw Bad();
            var anchors=new Dictionary<string,Vector2>(StringComparer.Ordinal);
            foreach(var token in nodes)
            {
                if(!(token is JObject node))throw Bad();Fields(node,"node_id","anchor","pick_shape_key");
                var id=Text(node,"node_id");if(!expected.Contains(id) || anchors.ContainsKey(id))throw Bad();
                Equal(node,"pick_shape_key","marker.circle");anchors.Add(id,Point(node["anchor"]));
            }
            Fields(land,"schema_version","dataset_id","description","projection","rings");Version(land);
            Equal(land,"dataset_id","presentation.natural_earth.land");Equal(land,"projection","equirectangular");Text(land,"description");
            if(!(land["rings"] is JArray rings) || rings.Count<1 || rings.Count>512)throw Bad();
            var geometry=new List<IReadOnlyList<Vector2>>();int total=0;
            foreach(var token in rings)
            {
                if(!(token is JArray ring) || ring.Count<4 || ring.Count>20000 || (total+=ring.Count)>20000)throw Bad();
                var points=ring.Select(Point).ToArray();
                if(!points[0].Equals(points[points.Length-1]))throw Bad();
                geometry.Add(Array.AsReadOnly(points));
            }
            return new WorldMapAssets {Anchors=new ReadOnlyDictionary<string,Vector2>(anchors),LandRings=geometry.AsReadOnly()};
        }
        static JObject Parse(string json)
        {
            if(string.IsNullOrWhiteSpace(json) || json.Length>524288)throw Bad();
            try
            {
                // Scan before allocation of the DOM; refuse comments, excess depth/work and extra roots.
                using(var scan=new JsonTextReader(new StringReader(json)){MaxDepth=12,DateParseHandling=DateParseHandling.None})
                { int count=0;while(scan.Read())if(scan.TokenType==JsonToken.Comment || ++count>100000)throw Bad(); }
                using(var reader=new JsonTextReader(new StringReader(json)){MaxDepth=12,DateParseHandling=DateParseHandling.None})
                    return JObject.Load(reader,new JsonLoadSettings {DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            }
            catch(JsonException e) { throw new ArgumentException("Invalid world map JSON.",e); }
        }
        static Vector2 Point(JToken token)
        {
            if(!(token is JArray point) || point.Count!=2)throw Bad();
            return new Vector2(Coordinate(point[0]),Coordinate(point[1]));
        }
        static float Coordinate(JToken token)
        {
            if(token==null || (token.Type!=JTokenType.Float && token.Type!=JTokenType.Integer))throw Bad();
            double value;
            try { value=token.Value<double>(); } catch(Exception e) when(e is OverflowException || e is InvalidCastException || e is FormatException) { throw Bad(); }
            if(double.IsNaN(value) || double.IsInfinity(value) || value<0 || value>1)throw Bad();return (float)value;
        }
        static void Fields(JObject node,params string[] fields)
        { if(node.Count!=fields.Length || fields.Any(f=>node.Property(f)==null))throw Bad(); }
        static string Text(JObject node,string field)
        { var t=node[field];if(t==null || t.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)t) || ((string)t).Length>2048)throw Bad();return (string)t; }
        static void Equal(JObject node,string field,string value) { if(Text(node,field)!=value)throw Bad(); }
        static void Version(JObject node)
        { if(node["schema_version"]?.Type!=JTokenType.Integer || node["schema_version"].ToString()!="1")throw Bad(); }
        static ArgumentException Bad()=>new ArgumentException("Unsupported or invalid world map presentation data.");
    }
}
