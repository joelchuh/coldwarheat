using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    // Implements only the keywords in the repository map contract, not arbitrary JSON Schema.
    public static class ContractJson
    {
        public static JToken Parse(string json)
        {
            if(json==null || json.Length>2000000) throw new ArgumentException("JSON size limit.");
            try
            {
                using(var reader=new JsonTextReader(new StringReader(json)) { DateParseHandling=DateParseHandling.None, MaxDepth=64 })
                {
                    var result=JToken.ReadFrom(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                    if(reader.Read()) throw new ArgumentException("Trailing JSON content.");
                    return result;
                }
            }
            catch(JsonException e) { throw new ArgumentException("Invalid JSON.",e); }
        }
        public static void Validate(JToken value,JObject schema)
        {
            var known=new[]{"$schema","$id","title","description","type","properties","required","additionalProperties","items","minItems","maxItems","minLength","minimum","pattern","const","enum","anyOf","uniqueItems"};
            if(schema.Properties().Any(p=>!known.Contains(p.Name))) throw new ArgumentException("Unsupported contract keyword.");
            if(schema["anyOf"] is JArray alternatives)
            {
                foreach(JObject a in alternatives) { try { Validate(value,a); return; } catch(ArgumentException) {} }
                throw new ArgumentException("No contract alternative matches.");
            }
            if(schema["type"]!=null)
            {
                var types=schema["type"] is JArray t?t.Values<string>().ToArray():new[]{(string)schema["type"]};
                if(!types.Any(t=>Matches(value,t))) throw new ArgumentException("Contract type mismatch.");
            }
            if(schema["const"]!=null && !JToken.DeepEquals(value,schema["const"])) throw new ArgumentException("Unsupported contract value.");
            if(schema["enum"] is JArray choices && !choices.Any(x=>JToken.DeepEquals(value,x))) throw new ArgumentException("Unknown enum.");
            if(value.Type==JTokenType.String)
            {
                var s=(string)value;
                if(schema["minLength"]!=null && s.Length<(int)schema["minLength"]) throw new ArgumentException("Empty contract string.");
                if(schema["pattern"]!=null && !Regex.IsMatch(s,(string)schema["pattern"],RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1))) throw new ArgumentException("Invalid contract ID.");
            }
            if(schema["minimum"]!=null && (double)value<(double)schema["minimum"]) throw new ArgumentException("Below minimum.");
            if(value is JArray array)
            {
                if(schema["minItems"]!=null && array.Count<(int)schema["minItems"] || schema["maxItems"]!=null && array.Count>(int)schema["maxItems"]) throw new ArgumentException("Array size.");
                if((bool?)schema["uniqueItems"]==true && array.Distinct(new JTokenEqualityComparer()).Count()!=array.Count) throw new ArgumentException("Duplicate item.");
                if(schema["items"] is JObject itemSchema) foreach(var item in array) Validate(item,itemSchema);
            }
            if(value is JObject obj)
            {
                var properties=schema["properties"] as JObject;
                foreach(string key in (schema["required"] as JArray ?? new JArray()).Values<string>())
                    if(obj.Property(key)==null) throw new ArgumentException("Missing field: "+key);
                foreach(var p in obj.Properties())
                {
                    if(properties?[p.Name] is JObject propertySchema) Validate(p.Value,propertySchema);
                    else if((bool?)schema["additionalProperties"]==false) throw new ArgumentException("Unknown field: "+p.Name);
                }
            }
        }
        static bool Matches(JToken value,string type)
        {
            switch(type)
            {
                case "null": return value.Type==JTokenType.Null;
                case "object": return value is JObject;
                case "array": return value is JArray;
                case "string": return value.Type==JTokenType.String;
                case "boolean": return value.Type==JTokenType.Boolean;
                case "integer": return value.Type==JTokenType.Integer || value.Type==JTokenType.Float && (double)value==Math.Truncate((double)value);
                default: throw new ArgumentException("Unsupported contract type.");
            }
        }
    }
}
