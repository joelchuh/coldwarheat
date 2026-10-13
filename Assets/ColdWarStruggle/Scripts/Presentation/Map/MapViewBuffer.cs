using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public class MapViewBuffer : IMapView
    {
        public event Action<MapIntent> Intent;
        public MapViewContext Context { get; private set; }
        public MapFrame Frame { get; private set; }
        public CountryId? FocusedCountry { get; private set; }
        public bool IsDisposed { get; private set; }
        HashSet<CountryId> known=new HashSet<CountryId>();
        long lastVersion=-1,lastSequence;
        public virtual void Initialize(MapViewContext context)
        {
            ThrowIfDisposed();Clear();Context=null;known.Clear();lastVersion=-1;lastSequence=0;
            Context=context??throw new ArgumentNullException(nameof(context));known=new HashSet<CountryId>(context.Nodes.Select(n=>n.Id));
        }
        public bool Present(MapFrame frame)
        {
            if(IsDisposed || Context==null || frame==null || frame.ContextId!=Context.Id || frame.Countries.Count!=known.Count || frame.Countries.Any(c=>!known.Contains(c.Id)))return false;
            if(frame.Version<lastVersion || frame.Sequence<=lastSequence)return false;
            Frame=frame;lastVersion=frame.Version;lastSequence=frame.Sequence;return true;
        }
        public void FocusOn(CountryId country)
        {
            ThrowIfDisposed();if(Context==null || !known.Contains(country))throw new ArgumentException("Unknown map focus.");FocusedCountry=country;
        }
        public bool TrySelect(CountryId country)
        {
            if(IsDisposed || Frame==null || !known.Contains(country))return false;
            var current=Frame;Intent?.Invoke(new MapIntent(current.ContextId,current.Version,current.Sequence,current.InteractionKey,country));return true;
        }
        public void Clear() { Frame=null;FocusedCountry=null; }
        public virtual void Dispose()
        { if(IsDisposed)return;IsDisposed=true;Clear();Context=null;known.Clear();Intent=null; }
        void ThrowIfDisposed() { if(IsDisposed)throw new ObjectDisposedException(nameof(MapViewBuffer)); }
    }
}
