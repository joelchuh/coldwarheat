using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;

namespace ColdWarStruggle.Presentation
{
    public sealed class MapNode
    {
        public CountryId Id { get; }
        public string Name { get; }
        public string Region { get; }
        public int Stability { get; }
        public bool Battleground { get; }
        public MapNode(CountryId id,string name,string region,int stability,bool battleground)
        {
            if(string.IsNullOrWhiteSpace(id.Value) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(region) || stability<1)throw new ArgumentException("Invalid public map node.");
            Id=id;Name=name;Region=region;Stability=stability;Battleground=battleground;
        }
    }
    public sealed class MapViewContext
    {
        public string Id { get; }
        public IReadOnlyList<MapNode> Nodes { get; }
        public MapViewContext(string id,IEnumerable<MapNode> nodes)
        {
            if(string.IsNullOrWhiteSpace(id) || nodes==null)throw new ArgumentException("Missing map view context.");
            var copy=nodes.ToArray();
            if(copy.Length==0 || copy.Any(n=>n==null) || copy.Select(n=>n.Id).Distinct().Count()!=copy.Length)throw new ArgumentException("Invalid node mapping.");
            Id=id;Nodes=Array.AsReadOnly(copy);
        }
    }
    public sealed class MapCountryFrame
    {
        public CountryId Id { get; }
        public int US { get; }
        public int USSR { get; }
        public PlayerSide? Controller { get; }
        public bool Allowed { get; }
        public bool Drafted { get; }
        public bool Selected { get; }
        public MapCountryFrame(CountryId id,int us,int ussr,PlayerSide? controller,bool allowed,bool drafted,bool selected)
        {
            if(string.IsNullOrWhiteSpace(id.Value) || us<0 || ussr<0 || (controller.HasValue && controller!=PlayerSide.US && controller!=PlayerSide.USSR))throw new ArgumentException("Invalid public country frame.");
            Id=id;US=us;USSR=ussr;Controller=controller;Allowed=allowed;Drafted=drafted;Selected=selected;
        }
    }
    public sealed class MapFrame
    {
        public string ContextId { get; }
        public long Version { get; }
        public long Sequence { get; }
        public string InteractionKey { get; }
        public IReadOnlyList<MapCountryFrame> Countries { get; }
        public MapFrame(string context,long version,long sequence,string interaction,IEnumerable<MapCountryFrame> countries)
        {
            if(string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(interaction) || version<0 || sequence<1 || countries==null)throw new ArgumentException("Invalid map frame stamp.");
            var copy=countries.ToArray();
            if(copy.Length==0 || copy.Any(c=>c==null) || copy.Select(c=>c.Id).Distinct().Count()!=copy.Length || copy.Count(c=>c.Selected)>1)throw new ArgumentException("Invalid country frame mapping.");
            ContextId=context;Version=version;Sequence=sequence;InteractionKey=interaction;Countries=Array.AsReadOnly(copy);
        }
    }
    public sealed class MapIntent
    {
        public string ContextId { get; }
        public long Version { get; }
        public long Sequence { get; }
        public string InteractionKey { get; }
        public CountryId Country { get; }
        public MapIntent(string context,long version,long sequence,string interaction,CountryId country)
        { ContextId=context;Version=version;Sequence=sequence;InteractionKey=interaction;Country=country; }
    }
    // v1 handles already-loaded diagnostic data; resource/animation APIs remain separate design work.
    public interface IMapView : IDisposable
    {
        event Action<MapIntent> Intent;
        void Initialize(MapViewContext context);
        bool Present(MapFrame frame);
        void FocusOn(CountryId country);
        void Clear();
    }
}
