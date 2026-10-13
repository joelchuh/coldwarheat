using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    // A bounded evaluator for the repository's supported contracts, not arbitrary JSON Schema.
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
            if(value==null || schema==null)throw new ArgumentException("Missing contract or value.");
            int budget=100000;
            CheckSchema(schema,schema,new HashSet<JObject>(),0,ref budget);
            ValidateNode(value,schema,schema,0,ref budget);
        }
        static readonly string[] KnownKeywords={"$schema","$id","$defs","$ref","title","description","type","properties","required","additionalProperties","items","minItems","maxItems","minLength","minimum","maximum","pattern","const","enum","anyOf","oneOf","allOf","if","then","else","uniqueItems"};
        static readonly string[] KnownTypes={"null","object","array","string","boolean","integer"};
        static void CheckKeywordShapes(JObject schema)
        {
            bool Strings(JToken token) => token is JArray a && a.All(x=>x.Type==JTokenType.String);
            foreach(var p in schema.Properties())
            {
                var v=p.Value;
                bool valid;
                switch(p.Name)
                {
                    case "$schema": case "$id": case "$ref": case "title": case "description": case "pattern":
                        valid=v.Type==JTokenType.String;break;
                    case "$defs": case "properties": case "items": case "if": case "then": case "else": valid=v is JObject;break;
                    case "additionalProperties": case "uniqueItems": valid=v.Type==JTokenType.Boolean;break;
                    case "required": valid=Strings(v) && ((JArray)v).Distinct(new JTokenEqualityComparer()).Count()==((JArray)v).Count;break;
                    case "anyOf": case "oneOf": case "allOf": valid=v is JArray alternatives && alternatives.Count>0 && alternatives.All(x=>x is JObject);break;
                    case "enum": valid=v is JArray choices && choices.Count>0;break;
                    case "type":
                        valid=v.Type==JTokenType.String ? KnownTypes.Contains((string)v)
                            : Strings(v) && ((JArray)v).Count>0 && ((JArray)v).Values<string>().All(KnownTypes.Contains);break;
                    case "minItems": case "maxItems": case "minLength":
                        valid=v.Type==JTokenType.Integer && (double)v>=0 && (double)v<=int.MaxValue;break;
                    case "minimum": case "maximum": valid=v.Type==JTokenType.Integer || v.Type==JTokenType.Float;break;
                    case "const": valid=true;break;
                    default: valid=false;break;
                }
                if(!valid)throw new ArgumentException("Unsupported contract keyword shape: "+p.Name);
            }
            if(schema["pattern"]!=null)new Regex((string)schema["pattern"],RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        }
        sealed class ContractLimitException : ArgumentException { public ContractLimitException() : base("Contract complexity limit.") {} }
        static void Limit(int depth,ref int budget)
        { if(depth>64 || --budget<0)throw new ContractLimitException(); }
        static JObject LocalReference(JObject schema,JObject root)
        {
            var reference=(string)schema["$ref"];
            const string prefix="#/$defs/";
            if(reference==null || !reference.StartsWith(prefix,StringComparison.Ordinal))throw new ArgumentException("Only local definition references are supported.");
            var name=reference.Substring(prefix.Length);
            if(name.Length==0 || name.Contains("/") || name.Contains("~") || root["$defs"]?[name] is not JObject target)
                throw new ArgumentException("Unknown local definition reference.");
            return target;
        }
        static void CheckSchema(JObject schema,JObject root,HashSet<JObject> active,int depth,ref int budget)
        {
            Limit(depth,ref budget);
            if(!active.Add(schema))throw new ArgumentException("Recursive contracts are not supported.");
            try
            {
                if(schema.Properties().Any(p=>!KnownKeywords.Contains(p.Name)))throw new ArgumentException("Unsupported contract keyword.");
                CheckKeywordShapes(schema);
                if(schema["$ref"]!=null)CheckSchema(LocalReference(schema,root),root,active,depth+1,ref budget);
                foreach(var group in new[]{"$defs","properties"})
                    if(schema[group] is JObject children)
                        foreach(var p in children.Properties())
                        {
                            if(p.Value is not JObject child)throw new ArgumentException("Invalid contract object.");
                            CheckSchema(child,root,active,depth+1,ref budget);
                        }
                foreach(var keyword in new[]{"items","if","then","else"})
                    if(schema[keyword] is JObject child)CheckSchema(child,root,active,depth+1,ref budget);
                foreach(var keyword in new[]{"anyOf","oneOf","allOf"})
                if(schema[keyword] is JArray alternatives)
                    foreach(var item in alternatives)
                    {
                        if(item is not JObject child)throw new ArgumentException("Invalid contract alternative.");
                        CheckSchema(child,root,active,depth+1,ref budget);
                    }
            }
            finally { active.Remove(schema); }
        }
        static void ValidateNode(JToken value,JObject schema,JObject root,int depth,ref int budget)
        {
            Limit(depth,ref budget);
            if(schema["$ref"]!=null)ValidateNode(value,LocalReference(schema,root),root,depth+1,ref budget);
            if(schema["allOf"] is JArray combined)
                foreach(JObject constraint in combined)ValidateNode(value,constraint,root,depth+1,ref budget);
            if(schema["if"] is JObject condition)
            {
                bool matched=true;
                try { ValidateNode(value,condition,root,depth+1,ref budget); }
                catch(ArgumentException e) when(e is not ContractLimitException) { matched=false; }
                if(schema[matched?"then":"else"] is JObject branch)ValidateNode(value,branch,root,depth+1,ref budget);
            }
            if(schema["anyOf"] is JArray alternatives)
            {
                bool matched=false;
                foreach(JObject a in alternatives)
                {
                    try { ValidateNode(value,a,root,depth+1,ref budget);matched=true;break; }
                    catch(ArgumentException e) when(e is not ContractLimitException) {}
                }
                if(!matched)throw new ArgumentException("No contract alternative matches.");
            }
            if(schema["oneOf"] is JArray exclusive)
            {
                int matches=0;
                foreach(JObject alternative in exclusive)
                {
                    try { ValidateNode(value,alternative,root,depth+1,ref budget);matches++; }
                    catch(ArgumentException e) when(e is not ContractLimitException) {}
                }
                if(matches!=1)throw new ArgumentException("Exactly one contract alternative must match.");
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
            if(schema["minimum"]!=null && (value.Type==JTokenType.Integer || value.Type==JTokenType.Float) && (double)value<(double)schema["minimum"]) throw new ArgumentException("Below minimum.");
            if(schema["maximum"]!=null && (value.Type==JTokenType.Integer || value.Type==JTokenType.Float) && (double)value>(double)schema["maximum"]) throw new ArgumentException("Above maximum.");
            if(value is JArray array)
            {
                if(schema["minItems"]!=null && array.Count<(int)schema["minItems"] || schema["maxItems"]!=null && array.Count>(int)schema["maxItems"]) throw new ArgumentException("Array size.");
                if((bool?)schema["uniqueItems"]==true && array.Distinct(new JTokenEqualityComparer()).Count()!=array.Count) throw new ArgumentException("Duplicate item.");
                if(schema["items"] is JObject itemSchema) foreach(var item in array) ValidateNode(item,itemSchema,root,depth+1,ref budget);
            }
            if(value is JObject obj)
            {
                var properties=schema["properties"] as JObject;
                foreach(string key in (schema["required"] as JArray ?? new JArray()).Values<string>())
                    if(obj.Property(key)==null) throw new ArgumentException("Missing field: "+key);
                foreach(var p in obj.Properties())
                {
                    if(properties?[p.Name] is JObject propertySchema) ValidateNode(p.Value,propertySchema,root,depth+1,ref budget);
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
